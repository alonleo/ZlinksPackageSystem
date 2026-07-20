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
            IProgress<string>? progress = null, CancellationToken ct = default)
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
                result.ErrorMessage = $"目录不是 Git 仓库（缺少 .git）：{repoRoot}";
                return result;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "-C", repoRoot, "pull", "--ff-only", "--progress" },
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

            if (proc.ExitCode == 0)
            {
                result.Success = true;
            }
            else
            {
                result.ErrorMessage = $"git pull 退出码 {proc.ExitCode}";
            }
            result.Logs = logs;
            return result;
        }
    }
}