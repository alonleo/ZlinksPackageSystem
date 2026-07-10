namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 参数输入类型
    /// </summary>
    public enum ToolArgumentInputType
    {
        Text,
        Number,
        Bool,
        File,
        Directory
    }

    /// <summary>
    /// 工具参数模型
    /// </summary>
    public class ToolArgument
    {
        /// <summary>参数名（如 --output / -c / --env）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>默认值（RequireInput=false 时使用）</summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>true=运行前弹窗让用户输入；false=直接用 DefaultValue</summary>
        public bool RequireInput { get; set; }

        /// <summary>输入类型，决定弹窗渲染什么控件</summary>
        public ToolArgumentInputType InputType { get; set; } = ToolArgumentInputType.Text;

        /// <summary>描述，弹窗时作为提示</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>拼接顺序</summary>
        public int Order { get; set; }
    }
}
