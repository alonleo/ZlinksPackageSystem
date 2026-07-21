using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    /// <summary>
    /// VenvService 的执行结果。
    /// 形态与 <see cref="CloneResult"/> 对齐,便于复用日志显示 UI。
    /// </summary>
    public class VenvResult
    {
        /// <summary>是否成功(venv 创建且 pip install 完成或跳过)</summary>
        public bool Success { get; set; }

        /// <summary>vnev 根目录绝对路径(创建时)</summary>
        public string VenvPath { get; set; } = string.Empty;

        /// <summary>venv 内可用的 python 解释器路径(Windows: pyvenv.cfg 同级 Scripts/python.exe;Unix: bin/python)</summary>
        public string PythonExePath { get; set; } = string.Empty;

        /// <summary>本次是否新创建了 venv(false 表示已存在,跳过创建步骤)</summary>
        public bool VenvCreated { get; set; }

        /// <summary>本次是否执行了 pip install</summary>
        public bool PipInstalled { get; set; }

        /// <summary>失败原因(用户可读)。成功时为空。</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>是否被取消</summary>
        public bool Cancelled { get; set; }

        /// <summary>逐步日志(供详情弹窗使用)</summary>
        public List<string> Logs { get; set; } = new();
    }
}
