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
    public class VenvService : IVenvService
    {
        public bool VenvExists(string venvDirectory)
        {
            if (string.IsNullOrWhiteSpace(venvDirectory)) return false;
            try
            {
                var root = Path.GetFullPath(venvDirectory);
                return File.Exists(Path.Combine(root, "pyvenv.cfg"));
            }
            catch
            {
                return false;
            }
        }

        public string ResolvePythonExePath(string venvDirectory)
        {
            if (string.IsNullOrWhiteSpace(venvDirectory)) return string.Empty;
            string root;
            try { root = Path.GetFullPath(venvDirectory); }
            catch { root = venvDirectory; }

            return OperatingSystem.IsWindows()
                ? Path.Combine(root, "Scripts", "python.exe")
                : Path.Combine(root, "bin", "python");
        }

        public async Task<VenvResult> EnsureVenvAsync(
            string pythonExe,
            string venvDirectory,
            string requirementsPath,
            string pipMirrorUrl,
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            var result = new VenvResult();
            var logs = new List<string>();
            void Log(string line)
            {
                if (string.IsNullOrEmpty(line)) return;
                logs.Add(line);
                try { progress?.Report(line); } catch { /* UI 已关闭 */ }
            }

            if (string.IsNullOrWhiteSpace(pythonExe))
            {
                result.ErrorMessage = "未配置 Python 解释器路径,无法创建虚拟环境。";
                Log("[venv] " + result.ErrorMessage);
                result.Logs = logs;
                return result;
            }
            if (string.IsNullOrWhiteSpace(venvDirectory))
            {
                result.ErrorMessage = "虚拟环境目录为空。";
                Log("[venv] " + result.ErrorMessage);
                result.Logs = logs;
                return result;
            }

            string venvRoot;
            try { venvRoot = Path.GetFullPath(venvDirectory); }
            catch (Exception ex)
            {
                result.ErrorMessage = $"虚拟环境目录路径无效:{ex.Message}";
                Log("[venv] " + result.ErrorMessage);
                result.Logs = logs;
                return result;
            }
            result.VenvPath = venvRoot;
            result.PythonExePath = ResolvePythonExePath(venvRoot);

            // ===== Step 1: 创建 venv(若不存在) =====
            if (VenvExists(venvRoot))
            {
                Log($"[venv] 虚拟环境已存在,跳过创建:{venvRoot}");
            }
            else
            {
                Log($"[venv] 正在创建虚拟环境:{venvRoot}");
                Log($"[venv] $ \"{pythonExe}\" -m venv \"{venvRoot}\"");
                var createResult = await RunProcessAsync(
                    pythonExe,
                    new[] { "-m", "venv", venvRoot },
                    Log,
                    ct);
                if (createResult.Cancelled)
                {
                    result.Cancelled = true;
                    result.ErrorMessage = "已取消";
                    result.Logs = logs;
                    return result;
                }
                if (!createResult.Success)
                {
                    result.ErrorMessage = $"python -m venv 失败:退出码 {createResult.ExitCode}\n{createResult.ErrorSummary}";
                    Log("[venv] " + result.ErrorMessage);
                    result.Logs = logs;
                    return result;
                }
                if (!VenvExists(venvRoot))
                {
                    result.ErrorMessage = $"python -m venv 执行成功但未生成 pyvenv.cfg,请检查 Python 安装。";
                    Log("[venv] " + result.ErrorMessage);
                    result.Logs = logs;
                    return result;
                }
                result.VenvCreated = true;
                Log("[venv] 虚拟环境创建成功。");
            }

            // ===== Step 2: pip install -r requirements(可选) =====
            if (string.IsNullOrWhiteSpace(requirementsPath))
            {
                Log("[venv] 未指定 requirements.txt,跳过依赖安装。");
                result.Success = true;
                result.Logs = logs;
                return result;
            }

            // 解析相对路径:相对于 CWD,这里不强求 workingDir;让用户填绝对路径或与 CWD 相对的
            var reqAbs = requirementsPath;
            try
            {
                if (!Path.IsPathRooted(reqAbs)) reqAbs = Path.GetFullPath(reqAbs);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"requirements.txt 路径无效:{ex.Message}";
                Log("[venv] " + result.ErrorMessage);
                result.Logs = logs;
                return result;
            }
            if (!File.Exists(reqAbs))
            {
                result.ErrorMessage = $"requirements.txt 不存在:{reqAbs}";
                Log("[venv] " + result.ErrorMessage);
                result.Logs = logs;
                return result;
            }

            Log($"[venv] 正在安装依赖:-r {reqAbs}" +
                (string.IsNullOrWhiteSpace(pipMirrorUrl) ? string.Empty : $" -i {pipMirrorUrl}"));

            var pipArgs = new List<string> { "-m", "pip", "install", "-r", reqAbs };
            if (!string.IsNullOrWhiteSpace(pipMirrorUrl))
                pipArgs.AddRange(new[] { "-i", pipMirrorUrl });

            var pipCmd = $"\"{result.PythonExePath}\" {string.Join(" ", pipArgs.Select(a => a.Contains(' ') ? "\"" + a + "\"" : a))}";
            Log("[venv] $ " + pipCmd);

            var pipResult = await RunProcessAsync(result.PythonExePath, pipArgs, Log, ct);
            if (pipResult.Cancelled)
            {
                result.Cancelled = true;
                result.ErrorMessage = "已取消";
                result.Logs = logs;
                return result;
            }
            if (!pipResult.Success)
            {
                result.ErrorMessage = $"pip install 失败:退出码 {pipResult.ExitCode}\n{pipResult.ErrorSummary}";
                Log("[venv] " + result.ErrorMessage);
                result.Logs = logs;
                return result;
            }

            result.PipInstalled = true;
            Log("[venv] 依赖安装成功。");
            result.Success = true;
            result.Logs = logs;
            return result;
        }

        // ============== 内部:运行进程并流式输出 ==============

        private class ProcOutcome
        {
            public bool Success { get; set; }
            public bool Cancelled { get; set; }
            public int ExitCode { get; set; }
            public string ErrorSummary { get; set; } = string.Empty;
        }

        private static async Task<ProcOutcome> RunProcessAsync(
            string fileName, IReadOnlyList<string> args,
            Action<string> log, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                if (!proc.Start())
                {
                    log($"[venv] 无法启动进程:{fileName}");
                    return new ProcOutcome { ErrorSummary = "无法启动进程" };
                }
            }
            catch (Exception ex)
            {
                log($"[venv] 启动进程失败:{ex.Message}");
                return new ProcOutcome { ErrorSummary = ex.Message };
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
                var errs = new List<string>();
                try
                {
                    string? line;
                    while ((line = await proc.StandardError.ReadLineAsync(ct)) != null)
                    {
                        log(line);
                        errs.Add(line);
                    }
                }
                catch (OperationCanceledException) { }
                catch { /* 流关闭 */ }
                return errs;
            }, ct);

            try { await proc.WaitForExitAsync(ct); }
            catch (OperationCanceledException)
            {
                return new ProcOutcome { Cancelled = true };
            }
            try { await Task.WhenAll(stdoutTask, stderrTask); } catch { /* 流截断 */ }
            var errsCollected = stderrTask.IsCompletedSuccessfully ? await stderrTask : new List<string>();

            return new ProcOutcome
            {
                ExitCode = proc.ExitCode,
                Success = proc.ExitCode == 0,
                ErrorSummary = errsCollected.Count == 0 ? string.Empty : string.Join("\n", errsCollected.TakeLast(5))
            };
        }
    }
}
