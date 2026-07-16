namespace ZlinksPackageSystem.Desktop.Models
{
    public class Platform
    {
        public long Id { get; set; }
        public string PlatformName { get; set; } = string.Empty;
        public string PlatformCode { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string Status { get; set; } = "active";
        public string Remark { get; set; } = string.Empty;
        public string? CreateTime { get; set; }
        public string? UpdateTime { get; set; }

        public bool IsMapped => ParamRouteRegistry.IsKnownCode(PlatformCode);
    }
}