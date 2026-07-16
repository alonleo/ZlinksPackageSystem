namespace ZlinksPackageSystem.Desktop.Models
{
    public class Product
    {
        public long Id { get; set; }
        public long? CopyrightId { get; set; }
        public long? GameId { get; set; }
        public long? CompanyId { get; set; }
        public long? PlatformId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string SdkVersion { get; set; } = string.Empty;
        public string ApkVersion { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public string PackageMode { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string Remark { get; set; } = string.Empty;
        public string? CopyrightName { get; set; }
        public string? GameName { get; set; }
        public string? CompanyName { get; set; }
        public string? PlatformName { get; set; }
        public string? CreateTime { get; set; }
        public string? UpdateTime { get; set; }
    }
}