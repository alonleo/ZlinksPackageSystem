using System;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class ToolProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
    }
}
