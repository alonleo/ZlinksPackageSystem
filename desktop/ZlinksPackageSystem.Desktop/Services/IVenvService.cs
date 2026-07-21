using System;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// Python 虚拟环境创建与依赖安装服务。
    /// 负责:<c>python -m venv &lt;dir&gt;</c> 与 <c>pip install -r requirements.txt -i mirror</c>。
    /// 模式参考 <see cref="GitService"/>。
    /// </summary>
    public interface IVenvService
    {
        /// <summary>
        /// 确保 venv 存在:若不存在则创建,然后按需 pip install。
        /// 全程通过 progress 回调推送日志。
        /// </summary>
        /// <param name="pythonExe">用于创建 venv 的 Python 解释器绝对路径(系统级或上一级)。</param>
        /// <param name="venvDirectory">venv 根目录(绝对路径或相对路径)。</param>
        /// <param name="requirementsPath">requirements.txt 路径(可空)。</param>
        /// <param name="pipMirrorUrl">pip 镜像源 URL(可空)。</param>
        Task<VenvResult> EnsureVenvAsync(
            string pythonExe,
            string venvDirectory,
            string requirementsPath,
            string pipMirrorUrl,
            IProgress<string>? progress = null,
            CancellationToken ct = default);

        /// <summary>
        /// 解析 venv 内的 python 解释器绝对路径。
        /// Windows: &lt;dir&gt;/Scripts/python.exe;Unix: &lt;dir&gt;/bin/python。
        /// </summary>
        string ResolvePythonExePath(string venvDirectory);

        /// <summary>
        /// 通过 <c>pyvenv.cfg</c> 文件检测给定目录是否已是一个完整的 Python 虚拟环境。
        /// </summary>
        bool VenvExists(string venvDirectory);
    }
}
