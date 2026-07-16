using System;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IGitService
    {
        /// <summary>检测本机 Git：git --version + where/which git</summary>
        Task<GitEnvironmentInfo> DetectAsync(CancellationToken ct = default);

        /// <summary>
        /// 后台克隆。stderr 通过 progress 推送。ct 取消时杀进程、返回 Cancelled=true。
        /// </summary>
        Task<CloneResult> CloneAsync(string url, string targetParentDir,
            IProgress<string>? progress = null, CancellationToken ct = default);
    }
}