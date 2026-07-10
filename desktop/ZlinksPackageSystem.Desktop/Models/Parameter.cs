using System;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// 参数数据模型
    /// </summary>
    public class Parameter
    {
        public int Id { get; set; }
        public string ParameterName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
