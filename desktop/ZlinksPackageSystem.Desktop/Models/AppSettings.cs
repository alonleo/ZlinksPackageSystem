using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark";
        public bool AutoCheckUpdates { get; set; } = true;
        public int BackgroundType { get; set; }
        public string BackgroundValue { get; set; } = string.Empty;
        public double BackgroundOpacity { get; set; } = 0.3;

        // 字体设置
        public string FontFamily { get; set; } = string.Empty;
        public double FontSize { get; set; } = 14;
        public bool FontIsBold { get; set; } = false;
        public bool FontIsItalic { get; set; } = false;

        // 已加载的自定义字体文件路径(完整路径,持久化于本地 Fonts 目录)
        public List<string> CustomFontFiles { get; set; } = new();
    }
}