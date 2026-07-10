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
    /// <summary>
    /// 编程语言运行时环境检测服务实现
    /// </summary>
    public class RuntimeEnvironmentService : IRuntimeEnvironmentService
    {
        // 启动时一次性检测结果缓存
        private readonly Dictionary<string, RuntimeEnvironment> _cache = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        // 受支持的语言列表
                public static readonly IReadOnlyList<RuntimeEnvironment> SupportedLanguages = new List<RuntimeEnvironment>
                {
                    new() { Language = "python",     DisplayName = "Python",       Version = "", ExecutablePath = "python",     Icon = "🐍" },
                    new() { Language = "python3",    DisplayName = "Python 3",     Version = "", ExecutablePath = "python3",    Icon = "🐍" },
                    new() { Language = "node",       DisplayName = "Node.js",      Version = "", ExecutablePath = "node",       Icon = "🟢" },
                    new() { Language = "java",       DisplayName = "Java",         Version = "", ExecutablePath = "java",       Icon = "☕" },
                    new() { Language = "go",         DisplayName = "Go",           Version = "", ExecutablePath = "go",         Icon = "🔵" },
                    new() { Language = "powershell", DisplayName = "PowerShell",   Version = "", ExecutablePath = "powershell", Icon = "💲" },
                    new() { Language = "bash",       DisplayName = "Bash",         Version = "", ExecutablePath = "bash",       Icon = "🐚" },
                    new() { Language = "dotnet",     DisplayName = ".NET CLI",     Version = "", ExecutablePath = "dotnet",     Icon = "🟣" }
                };

        public async Task<List<RuntimeEnvironment>> DetectAllAsync()
        {
            var results = new List<RuntimeEnvironment>();
            foreach (var lang in SupportedLanguages)
            {
                var env = await DetectInternalAsync(lang.Language, useCache: true);
                results.Add(env);
            }
            return results;
        }

        public async Task<RuntimeEnvironment> DetectAsync(string language)
        {
            return await DetectInternalAsync(language, useCache: true);
        }

        public async Task<RuntimeEnvironment> ReDetectAsync(string language)
        {
            return await DetectInternalAsync(language, useCache: false);
        }

        private async Task<RuntimeEnvironment> DetectInternalAsync(string language, bool useCache)
        {
            if (string.IsNullOrWhiteSpace(language))
                return new RuntimeEnvironment { Language = language, IsAvailable = false };

            await _lock.WaitAsync();
            try
            {
                if (useCache && _cache.TryGetValue(language, out var cached))
                    return cached;
            }
            finally
            {
                _lock.Release();
            }

            // Windows 上 powershell 用 powershell.exe
            string[] candidates = GetExecutableCandidates(language);
            string? foundPath = null;
            string versionOutput = string.Empty;

            foreach (var exe in candidates)
            {
                // 先用 where 找绝对路径
                var fullPath = await ResolveExecutableAsync(exe);
                if (string.IsNullOrEmpty(fullPath)) continue;

                // 再跑 --version 确认可用
                var (success, output) = await RunVersionCheckAsync(fullPath, language);
                if (success)
                {
                    foundPath = fullPath;
                    versionOutput = output;
                    break;
                }
            }

            var template = SupportedLanguages.FirstOrDefault(x => x.Language == language);
                        var displayBase = template?.DisplayName ?? language;
                        var icon = template?.Icon ?? "🔧";

                        var env = new RuntimeEnvironment
                        {
                            Language = language,
                            DisplayName = string.IsNullOrEmpty(versionOutput)
                                ? displayBase
                                : $"{displayBase} {versionOutput.Split('\n')[0].Trim()}",
                            Icon = icon,
                            Version = versionOutput.Split('\n')[0].Trim(),
                            ExecutablePath = foundPath ?? string.Empty,
                            IsAvailable = !string.IsNullOrEmpty(foundPath)
                        };

            await _lock.WaitAsync();
            try
            {
                _cache[language] = env;
            }
            finally
            {
                _lock.Release();
            }

            return env;
        }

        private static string[] GetExecutableCandidates(string language)
        {
            return language.ToLowerInvariant() switch
            {
                "python" or "python3" => new[] { "python", "python3", "py" },
                "node" => new[] { "node", "node.exe" },
                "java" => new[] { "java", "java.exe" },
                "go" => new[] { "go", "go.exe" },
                "powershell" => new[] { "powershell", "powershell.exe", "pwsh", "pwsh.exe" },
                "bash" => new[] { "bash", "bash.exe" },
                "dotnet" => new[] { "dotnet", "dotnet.exe" },
                _ => new[] { language }
            };
        }

        private static async Task<string?> ResolveExecutableAsync(string exe)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    Arguments = exe,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return null;
                var output = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
                if (p.ExitCode != 0) return null;
                var first = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return first?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static async Task<(bool success, string output)> RunVersionCheckAsync(string fullPath, string language)
        {
            string args = language.ToLowerInvariant() switch
            {
                "python" or "python3" => "--version",
                "node" => "--version",
                "java" => "-version",
                "go" => "version",
                "powershell" => "-Command \"$PSVersionTable.PSVersion.ToString()\"",
                "bash" => "--version",
                "dotnet" => "--version",
                _ => "--version"
            };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fullPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return (false, string.Empty);

                // 某些命令走 stderr（如 java -version、python --version）
                var stdout = await p.StandardOutput.ReadToEndAsync();
                var stderr = await p.StandardError.ReadToEndAsync();
                await p.WaitForExitAsync();

                if (p.ExitCode != 0) return (false, string.Empty);
                return (true, (stdout + stderr).Trim());
            }
            catch
            {
                return (false, string.Empty);
            }
        }
    }
}
