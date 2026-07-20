using System;
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class User
    {
        /// <summary>
        /// 用户 ID(与后端 Java Long 对齐,避免 > 2^31 时的精度丢失)
        /// </summary>
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long GroupId { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<string> DesktopModules { get; set; } = new();
    }
}