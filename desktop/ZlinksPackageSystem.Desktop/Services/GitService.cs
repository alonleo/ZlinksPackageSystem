using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class GitService : IGitService
    {
        public async Task<GitEnvironmentInfo> DetectAsync(CancellationToken ct = default)
        {
            var info = new GitEnvironmentInfo();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    ArgumentList = { "git" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p1 = Process.Start(psi);
                if (p1 == null) return info;
                var pathOut = await p1.StandardOutput.ReadToEndAsync(ct);
                await p1.WaitForExitAsync(ct);
                if (p1.ExitCode != 0) return info;
                var path = pathOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                if (string.IsNullOrEmpty(path)) return info;
                info.ExecutablePath = path;

                var verPsi = new ProcessStartInfo
                {
                    FileName = path,
                    ArgumentList = { "--version" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p2 = Process.Start(verPsi);
                if (p2 == null) return info;
                var verOut = await p2.StandardOutput.ReadToEndAsync(ct);
                await p2.WaitForExitAsync(ct);
                if (p2.ExitCode != 0) return info;
                info.Version = verOut.Trim().Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
                info.IsInstalled = !string.IsNullOrEmpty(info.Version);
                return info;
            }
            catch
            {
                return info;
            }
        }

        public async Task<CloneResult> CloneAsync(string url, string targetParentDir,
            IProgress<string>? progress = null, CancellationToken ct = default)
        {
            var logs = new List<string>();
            void Log(string? line)
            {
                if (string.IsNullOrEmpty(line)) return;
                logs.Add(line);
                try { progress?.Report(line); } catch { /* UI 已关闭 */ }
            }

            var result = new CloneResult();
            if (string.IsNullOrWhiteSpace(url))
            {
                result.ErrorMessage = "Git URL 为空";
                return result;
            }
            if (string.IsNullOrWhiteSpace(targetParentDir))
            {
                result.ErrorMessage = "目标目录为空";
                return result;
            }

            string repoName;
            try { repoName = GitUrlParser.ParseRepoName(url); }
            catch (ArgumentException ex) { result.ErrorMessage = ex.Message; return result; }

            var repoRoot = GitUrlParser.CombineRepoRoot(targetParentDir, repoName);
            result.RepoName = repoName;
            result.RepoRoot = repoRoot;

            if (Directory.Exists(targetParentDir)
                && Directory.EnumerateFileSystemEntries(targetParentDir).Any())
            {
                result.ErrorMessage = $"目标目录非空：{targetParentDir}\n请选择一个空目录或新建目录。";
                return result;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "clone", "--progress", url, targetParentDir },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                if (!proc.Start())
                {
                    result.ErrorMessage = "无法启动 git 进程";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"启动 git 失败：{ex.Message}";
                return result;
            }

            using var reg = ct.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                        proc.Kill(entireProcessTree: true);
                }
                catch { /* 已退出 */ }
            });

            var stdoutTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null)
                        Log(line);
                }
                catch (OperationCanceledException) { }
                catch { /* 流关闭 */ }
            }, ct);
            var stderrTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await proc.StandardError.ReadLineAsync(ct)) != null)
                        Log(line);
                }
                catch (OperationCanceledException) { }
                catch { /* 流关闭 */ }
            }, ct);

            try
            {
                await proc.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                result.Cancelled = true;
                result.ErrorMessage = "已取消";
                result.Logs = logs;
                return result;
            }

            try { await Task.WhenAll(stdoutTask, stderrTask); } catch { /* 流截断 */ }

            if (proc.ExitCode == 0 && Directory.Exists(repoRoot))
            {
                result.Success = true;
            }
            else
            {
                result.ErrorMessage = $"git clone 退出码 {proc.ExitCode}";
            }
            result.Logs = logs;
            return result;
        }

        public async Task<CloneResult> PullAsync(string repoRoot,
            IProgress<string>? progress = null, CancellationToken ct = default,
            string? initUrl = null, string initRemoteName = "origin")
        {
            var logs = new List<string>();
            void Log(string? line)
            {
                if (string.IsNullOrEmpty(line)) return;
                logs.Add(line);
                try { progress?.Report(line); } catch { /* UI 已关闭 */ }
            }

            var result = new CloneResult { RepoRoot = repoRoot };
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                result.ErrorMessage = "仓库根目录为空";
                return result;
            }
            if (!Directory.Exists(repoRoot))
            {
                result.ErrorMessage = $"仓库根目录不存在：{repoRoot}";
                return result;
            }
            var gitDir = Path.Combine(repoRoot, ".git");
            if (!Directory.Exists(gitDir))
            {
                // 缺 .git:若调用方给了 initUrl,先 init + remote add,再 pull
                if (initUrl != null && !string.IsNullOrWhiteSpace(initUrl))
                {
                    var ensure = await EnsureRemoteAsync(repoRoot, initUrl, initRemoteName,
                        initIfMissing: true, progress: progress, ct: ct);
                    if (!ensure.Success)
                    {
                        result.ErrorMessage = ensure.ErrorMessage.Length > 0
                            ? "拉取前初始化失败：" + ensure.ErrorMessage
                            : "拉取前初始化失败";
                        result.Logs = logs;
                        return result;
                    }
                    Log("[git] .git 已初始化并绑定远端,继续执行 git pull。");
                }
                else
                {
                    result.ErrorMessage = $"目录不是 Git 仓库（缺少 .git）：{repoRoot}";
                    return result;
                }
            }

            var pullPsi = new ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "-C", repoRoot, "pull", "--ff-only", "--progress" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var pullOutcome = await RunGitAsync(pullPsi, Log, ct);
            if (pullOutcome.Cancelled)
            {
                result.Cancelled = true;
                result.ErrorMessage = "已取消";
            }
            else if (pullOutcome.ExitCode != 0)
            {
                result.ErrorMessage = $"git pull 退出码 {pullOutcome.ExitCode}";
            }
            else
            {
                result.Success = true;
            }
            result.Logs = logs;
            return result;
        }

        public async Task<(string RemoteName, string? Url, List<string> Logs)> DetectRemoteAsync(
            string dir, string remoteName = "origin", CancellationToken ct = default)
        {
            var logs = new List<string>();
            if (string.IsNullOrWhiteSpace(dir))
            {
                logs.Add("[detect] 目录为空。");
                return (remoteName, null, logs);
            }
            if (string.IsNullOrWhiteSpace(remoteName)) remoteName = "origin";
            var gitConfigPath = Path.Combine(dir, ".git", "config");
            if (!File.Exists(gitConfigPath))
            {
                logs.Add($"[detect] 未找到 .git/config：{gitConfigPath}");
                return (remoteName, null, logs);
            }
            string content;
            try
            {
                content = await File.ReadAllTextAsync(gitConfigPath, ct);
            }
            catch (Exception ex)
            {
                logs.Add($"[detect] 读取 .git/config 失败:{ex.Message}");
                return (remoteName, null, logs);
            }
            var url = GitUrlParser.ParseRemoteUrl(content, remoteName);
            if (url == null)
            {
                logs.Add($"[detect] 未在 .git/config 中找到 [remote \"{remoteName}\"] 段的 url。");
                return (remoteName, null, logs);
            }
            logs.Add($"[detect] 已解析 [{remoteName}] url = {url}");
            return (remoteName, url, logs);
        }

        public async Task<CloneResult> EnsureRemoteAsync(
            string dir, string? url, string remoteName = "origin",
            bool initIfMissing = true, IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            var logs = new List<string>();
            void Log(string? line)
            {
                if (string.IsNullOrEmpty(line)) return;
                logs.Add(line);
                try { progress?.Report(line); } catch { /* UI 已关闭 */ }
            }

            var result = new CloneResult { RepoRoot = dir };
            if (string.IsNullOrWhiteSpace(dir))
            {
                result.ErrorMessage = "目录为空";
                result.Logs = logs;
                return result;
            }
            if (string.IsNullOrWhiteSpace(remoteName)) remoteName = "origin";

            string gitDir = Path.Combine(dir, ".git");
            bool needInit = initIfMissing && !Directory.Exists(gitDir);

            // Step 1: 如需 init
            if (needInit)
            {
                Log($"[ensure] {dir} 不是 git 仓库,执行 git init。");
                var initPsi = new ProcessStartInfo
                {
                    FileName = "git",
                    ArgumentList = { "-C", dir, "init" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var initOutcome = await RunGitAsync(initPsi, Log, ct);
                if (initOutcome.Cancelled)
                {
                    result.Cancelled = true;
                    result.ErrorMessage = "已取消";
                    result.Logs = logs;
                    return result;
                }
                if (initOutcome.ExitCode != 0)
                {
                    result.ErrorMessage = $"git init 退出码 {initOutcome.ExitCode}";
                    result.Logs = logs;
                    return result;
                }
                Log("[ensure] git init 完成。");
            }

            // Step 2: 仅当提供了 url 才绑 remote
            if (string.IsNullOrWhiteSpace(url))
            {
                Log("[ensure] 未提供 url,跳过 remote 配置。");
                result.Success = true;
                result.Logs = logs;
                return result;
            }

            // 检查现有 remote 列表,决定用 add 还是 set-url
            string subcmd;
            bool needList = true;
            try { needList = !Directory.Exists(gitDir); } catch { needList = true; }
            if (!needList)
            {
                var listPsi = new ProcessStartInfo
                {
                    FileName = "git",
                    ArgumentList = { "-C", dir, "remote", "get-url", remoteName },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var listOutcome = await RunGitAsync(listPsi, Log, ct);
                subcmd = listOutcome.ExitCode == 0 ? "remote set-url" : "remote add";
            }
            else
            {
                subcmd = "remote add";
            }
            Log($"[ensure] 执行 git {subcmd} {remoteName} {url}");
            string[] cmdArgs = subcmd == "remote set-url"
                ? new[] { "-C", dir, "remote", "set-url", remoteName, url! }
                : new[] { "-C", dir, "remote", "add", remoteName, url! };
            var cfgPsi = new ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var a in cmdArgs) cfgPsi.ArgumentList.Add(a);
            var cfgOutcome = await RunGitAsync(cfgPsi, Log, ct);
            if (cfgOutcome.Cancelled)
            {
                result.Cancelled = true;
                result.ErrorMessage = "已取消";
            }
            else if (cfgOutcome.ExitCode != 0)
            {
                result.ErrorMessage = $"git {subcmd} 退出码 {cfgOutcome.ExitCode}";
            }
            else
            {
                result.Success = true;
            }
            result.Logs = logs;
            return result;
        }

        // ============= 内部:运行 git 子进程并流式回传日志 =============

        private class ProcOutcome
        {
            public int ExitCode { get; set; }
            public bool Cancelled { get; set; }
        }

        private static async Task<ProcOutcome> RunGitAsync(
            ProcessStartInfo psi, Action<string?> log, CancellationToken ct)
        {
            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                if (!proc.Start())
                {
                    log("[git] 无法启动 git 进程");
                    return new ProcOutcome { ExitCode = -1 };
                }
            }
            catch (Exception ex)
            {
                log($"[git] 启动 git 失败:{ex.Message}");
                return new ProcOutcome { ExitCode = -1 };
            }
            using var reg = ct.Register(() =>
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
                catch { /* 已退出 */ }
            });
            var stdoutTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null) log(line);
                }
                catch (OperationCanceledException) { }
                catch { /* 流关闭 */ }
            }, ct);
            var stderrTask = Task.Run(async () =>
            {
                try
                {
                    string? line;
                    while ((line = await proc.StandardError.ReadLineAsync(ct)) != null) log(line);
                }
                catch (OperationCanceledException) { }
                catch { /* 流关闭 */ }
            }, ct);
            try { await proc.WaitForExitAsync(ct); }
            catch (OperationCanceledException)
            {
                return new ProcOutcome { Cancelled = true };
            }
            try { await Task.WhenAll(stdoutTask, stderrTask); } catch { /* 流截断 */ }
            return new ProcOutcome { ExitCode = proc.ExitCode };
        }
    }
}