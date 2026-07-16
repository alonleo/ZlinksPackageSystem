using System.Diagnostics;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 进程生命周期管理：启动后台进程并跟踪 PID，支持按 PID 杀进程。
    /// </summary>
    public interface IProcessManagerService
    {
        /// <summary>
        /// 启动一个进程并跟踪，进程退出时会自动从内部字典中移除。
        /// </summary>
        /// <param name="psi">启动参数</param>
        /// <param name="captureOutput">是否捕获 stdout/stderr（用于通知卡片）</param>
        /// <returns>启动的进程 PID。返回 0 表示启动失败。</returns>
        int Start(ProcessStartInfo psi, bool captureOutput = false);

        /// <summary>
        /// 按 PID 杀进程。先尝试 CloseMainWindow，2s 内未退出则强杀整棵进程树。
        /// </summary>
        /// <returns>true=找到并发出 kill；false=PID 不存在</returns>
        bool Kill(int processId);

        /// <summary>指定 PID 的进程是否仍在运行</summary>
        bool IsRunning(int processId);

        /// <summary>
        /// 获取已退出进程的捕获输出（captureOutput=true 时有效）。不存在返回空串。
        /// </summary>
        string GetOutput(int processId);

        /// <summary>
        /// 进程退出事件。参数：(PID, ExitCode)。
        /// 调用方需自行在 UI 线程 marshal。
        /// </summary>
        event System.Action<int, int>? ProcessExited;

        /// <summary>输出捕获事件（每 ~500ms 触发一次，含 PID）。订阅方负责 marshal 到 UI 线程。</summary>
        event System.Action<int>? OutputCaptured;
    }
}
