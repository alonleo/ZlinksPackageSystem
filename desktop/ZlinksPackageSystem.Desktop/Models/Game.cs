using System;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class Game
    {
        /// <summary>
        /// 游戏 ID(与后端 Java Long 对齐,避免 > 2^31 时的精度丢失)
        /// </summary>
        public long Id { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string GameDirection { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string GitUrl { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public string WhiteBranch { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RetentionRecord { get; set; } = string.Empty;
        public string AndroidFolderName { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}