using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);

        /// <summary>显示一个确认对话框(确定/取消),返回 true=确定,false=取消。</summary>
        Task<bool> ShowConfirmAsync(string title, string message,
            string okText = "确定", string cancelText = "取消");
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

        /// <summary>在指定目录里挑一个脚本文件（.py/.js/.ts/.java/.go/.ps1/.sh/.bat/.cmd）。返回 null=取消。</summary>
        Task<string?> PickScriptFileInDirectoryAsync(string directory);

        /// <summary>显示克隆日志详情弹窗（含可滚动只读日志 + 复制按钮）。</summary>
        Task ShowCloneLogAsync(string title, string message, IReadOnlyList<string> logs, bool success);

        /// <summary>
        /// 虚拟环境创建进度弹窗。在 workAsync 执行期间显示一个带取消按钮的进度窗口，
        /// 通过 IProgress&lt;string&gt; 实时汇报进度文字。workAsync 完成后窗口自动关闭。
        /// </summary>
        Task<VenvResult> ShowVenvProgressAsync(
            string title,
            Func<IProgress<string>, CancellationToken, Task<VenvResult>> workAsync,
            CancellationTokenSource cts);
    }
}
