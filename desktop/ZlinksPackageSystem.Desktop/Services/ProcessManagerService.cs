using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 进程管理服务实现。
    /// 关键点：
    ///   1. 维护 PID → Process 的字典
    ///   2. 启动时启用 Exited 事件，自动从字典中移除并回调
    ///   3. 杀进程：先优雅关主窗口 → 等待 → 强杀整棵树
    /// </summary>
    public class ProcessManagerService : IProcessManagerService
    {
        private readonly ConcurrentDictionary<int, Process> _processes = new();
        private readonly TimeSpan _gracefulKillTimeout = TimeSpan.FromSeconds(2);

        public event Action<int, int>? ProcessExited;

        public int Start(ProcessStartInfo psi)
        {
            if (psi == null) return 0;

            try
            {
                var proc = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };
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
                        try { ProcessExited?.Invoke(pid, code); } catch { /* 吞掉订阅者异常 */ }
                    }
                };

                if (!proc.Start())
                    return 0;

                int id = proc.Id;
                _processes[id] = proc;
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

                // 1) 尝试优雅关闭（GUI 程序才会响应 CloseMainWindow）
                try { proc.CloseMainWindow(); } catch { /* 控制台程序无主窗口，忽略 */ }

                // 2) 等待 gracefulKillTimeout
                if (proc.WaitForExit((int)_gracefulKillTimeout.TotalMilliseconds))
                    return true;

                // 3) 仍未退出 → 强杀整棵进程树
                try
                {
                    if (OperatingSystem.IsWindows())
                        proc.Kill(entireProcessTree: true);
                    else
                        proc.Kill(entireProcessTree: true);
                }
                catch { /* 已退出 */ }

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
    }
}
