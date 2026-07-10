using System.Collections.Generic;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);
        Task<bool> ShowNotificationDetailAsync(NotificationItem item);

        /// <summary>
        /// 一次性表单输入所有参数。返回参数名→用户输入值的映射；用户取消返回 null。
        /// </summary>
        Task<Dictionary<string, string>?> PromptArgumentsAsync(IEnumerable<ToolArgument> arguments);

        /// <summary>
        /// 显示进程执行输出结果（含命令、stdout、stderr、退出码、耗时）。
        /// </summary>
        Task ShowOutputAsync(string toolName, ProcessRunResult result);

        /// <summary>
        /// 环境检测结果弹窗（无系统装饰栏，底部留白，按钮带边距）。
        /// </summary>
        Task ShowEnvironmentResultAsync(string title, string message, bool success);

        /// <summary>
        /// 启动确认弹窗：显示完整命令、待询问参数（RequireInput=true）表格（可编辑值），
        /// 以及「不再询问」勾选框。
        /// 用户取消返回 null。
        /// </summary>
        Task<RunConfirmation?> ShowRunConfirmationAsync(
            ToolProject project,
            string initialCommandLine,
            IEnumerable<EditableArgument> arguments);
    }
}
