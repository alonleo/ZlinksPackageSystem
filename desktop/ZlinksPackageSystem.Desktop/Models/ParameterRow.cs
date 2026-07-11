namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 编辑弹窗中的「参数行」：用户在「前缀 / 参数名 / 参数值」三列直接填写一行参数。
    /// </summary>
    public class ParameterRow
    {
        /// <summary>参数前缀（如 "--"、"-"，缺省为 "--"）</summary>
        public string Prefix { get; set; } = "--";

        /// <summary>参数名（如 output、port、verbose）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>参数值（用户在编辑弹窗或运行时录入的字符串值）</summary>
        public string Value { get; set; } = string.Empty;
    }
}
