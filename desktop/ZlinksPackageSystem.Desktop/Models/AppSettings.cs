namespace ZlinksPackageSystem.Desktop.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark";
        public bool AutoCheckUpdates { get; set; } = true;
        public int BackgroundType { get; set; }
        public string BackgroundValue { get; set; } = string.Empty;
        public double BackgroundOpacity { get; set; } = 0.3;
    }
}
