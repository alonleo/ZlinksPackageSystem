namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
        /// 编程语言运行时环境检测结果
        /// </summary>
        public class RuntimeEnvironment
        {
            /// <summary>语言标识（与 ToolProject.Language 对应）：python / node / java / go / powershell / bash</summary>
            public string Language { get; set; } = string.Empty;

            /// <summary>显示名，如 "Python 3.11.5"</summary>
            public string DisplayName { get; set; } = string.Empty;

            /// <summary>默认图标（如 🐍 / ☕ / 🟢 等）</summary>
            public string Icon { get; set; } = "🔧";

            /// <summary>版本号字符串</summary>
            public string Version { get; set; } = string.Empty;

            /// <summary>解释器/运行时可执行文件绝对路径</summary>
            public string ExecutablePath { get; set; } = string.Empty;

            /// <summary>是否可用</summary>
            public bool IsAvailable { get; set; }

            /// <summary>UI 展开状态(运行期仅内存保留)</summary>
            public bool IsExpanded { get; set; }
        }
}
