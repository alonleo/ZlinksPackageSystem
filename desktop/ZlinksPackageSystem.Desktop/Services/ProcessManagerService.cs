using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 进程管理服务实现。
    /// 关键点：
    ///   1. 维护 PID → Process 的字典
    ///   2. 启动时启用 Exited 事件，自动从字典中移除并回调
    ///   3. 杀进程：先优雅关主窗口 → 等待 → 强杀整棵树
    ///   4. captureOutput=true 时同时把 stdout/stderr 持续缓冲到 _outputs
    /// </summary>
    public class ProcessManagerService : IProcessManagerService
    {
        private readonly ConcurrentDictionary<int, Process> _processes = new();
        private readonly ConcurrentDictionary<int, StringBuilder> _outputs = new();
        private readonly TimeSpan _gracefulKillTimeout = TimeSpan.FromSeconds(2);
        private readonly System.Timers.Timer _outputTimer;

        public event Action<int, int>? ProcessExited;
        public event Action<int>? OutputCaptured;

        public ProcessManagerService()
        {
            _outputTimer = new System.Timers.Timer(500) { AutoReset = true };
            _outputTimer.Elapsed += (_, _) =>
            {
                foreach (var key in _outputs.Keys)
                {
                    try { OutputCaptured?.Invoke(key); } catch { /* 吞订阅者异常 */ }
                }
            };
            _outputTimer.Start();
        }

        public int Start(ProcessStartInfo psi, bool captureOutput = false)
        {
            if (psi == null) return 0;

            if (captureOutput)
            {
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
            }

            try
            {
                var proc = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

                int id = 0;
                StringBuilder sb = new();
                if (captureOutput)
                {
                    proc.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data != null)
                        {
                            lock (sb) { sb.AppendLine(e.Data); }
                        }
                    };
                    proc.ErrorDataReceived += (_, e) =>
                    {
                        if (e.Data != null)
                        {
                            lock (sb) { sb.AppendLine(e.Data); }
                        }
                    };
                }

                proc.Exited += (_, _) =>
                {
                    int pid = 0;
                    int code = -1;
                    try
                    {
                        pid = proc.Id;
                        code = proc.ExitCode;
                    }
                    catch { /* 进程可能已释放 */ }
                    finally
                    {
                        _processes.TryRemove(pid, out _);
                        if (captureOutput)
                        {
                            _outputs[pid] = sb;
                        }
                        try { ProcessExited?.Invoke(pid, code); } catch { /* 吞掉订阅者异常 */ }
                    }
                };

                if (!proc.Start())
                    return 0;

                id = proc.Id;
                _processes[id] = proc;

                if (captureOutput)
                {
                    try { proc.BeginOutputReadLine(); } catch { }
                    try { proc.BeginErrorReadLine(); } catch { }
                    _outputs[id] = sb;
                }
                return id;
            }
            catch
            {
                return 0;
            }
        }

        public bool Kill(int processId)
        {
            if (!_processes.TryGetValue(processId, out var proc))
                return false;

            try
            {
                if (proc.HasExited)
                    return true;

                try { proc.CloseMainWindow(); } catch { }

                if (proc.WaitForExit((int)_gracefulKillTimeout.TotalMilliseconds))
                    return true;

                try
                {
                    if (OperatingSystem.IsWindows())
                        proc.Kill(entireProcessTree: true);
                    else
                        proc.Kill(entireProcessTree: true);
                }
                catch { }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsRunning(int processId)
        {
            if (!_processes.TryGetValue(processId, out var proc))
                return false;
            try
            {
                return !proc.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public string GetOutput(int processId)
        {
            return _outputs.TryGetValue(processId, out var sb) ? sb.ToString() : string.Empty;
        }
    }
}