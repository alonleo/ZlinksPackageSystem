using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 启动确认弹窗中「可编辑参数」一行。
    /// 用户可以修改 Value，Prefix 在 UseDefaultPrefix=true 时锁定为工具默认。
    /// </summary>
    public class EditableArgument
    {
        public ToolArgument Source { get; init; } = null!;

        /// <summary>当前显示的前缀（UseDefaultPrefix=true 时为工具默认；否则为用户自定义）</summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>用户编辑后的值</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>true=使用工具级 DefaultArgumentPrefix；false=使用 Prefix 自定义</summary>
        public bool UseDefaultPrefix { get; set; } = true;

        public string DisplayName => string.IsNullOrEmpty(Source.Description)
            ? Source.Name
            : $"{Source.Name}  ({Source.Description})";
    }

    /// <summary>
    /// 启动确认结果。用户取消时为 null。
    /// </summary>
    public class RunConfirmation
    {
        /// <summary>用户在弹窗中确认的参数列表</summary>
        public System.Collections.Generic.List<EditableArgument> Arguments { get; init; } = new();

        /// <summary>最终命令行（拼出来的预览）</summary>
        public string CommandLine { get; init; } = string.Empty;

        /// <summary>用户在弹窗里勾选「不再询问」，下次点运行直接启动</summary>
        public bool DoNotAskAgain { get; init; }
    }
}
