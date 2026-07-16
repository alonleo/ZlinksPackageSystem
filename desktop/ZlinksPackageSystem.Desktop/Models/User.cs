using System;
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<string> DesktopModules { get; set; } = new();
    }
}