namespace ZlinksPackageSystem.Desktop.Models
{
    public class AdParam
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string? PackageName { get; set; }
        public string? AppId { get; set; }
        public string? AppSecret { get; set; }
        public string? MediaId { get; set; }
        public string? ContractStatus { get; set; }
        public string? AgconnectPath { get; set; }
        public string? TdAppId { get; set; }
        public string? AdParamStatus { get; set; }
        public string? ListStatus { get; set; }
        public string? Operator { get; set; }
        public string? Remark { get; set; }
        public string? CreateTime { get; set; }
        public string? UpdateTime { get; set; }
    }
}