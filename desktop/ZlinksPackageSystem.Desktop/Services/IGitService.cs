using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 在已有仓库根目录上执行 <c>git pull --ff-only --progress</c>。
        /// stdout/stderr 通过 progress 推送；ct 取消时杀进程、返回 Cancelled=true。
        /// </summary>
        /// <param name="repoRoot">已存在的 git 仓库根目录(必须含 <c>.git</c>)。</param>
        /// <param name="initUrl">当目录缺少 <c>.git</c> 时:<c>git init</c> 后用此 URL + remoteName 执行 <c>git remote add</c>。null=保持原行为(缺 <c>.git</c> 直接报错)。</param>
        /// <param name="initRemoteName">与 <paramref name="initUrl"/> 配对的远端名,默认 <c>origin</c>。</param>
        Task<CloneResult> PullAsync(string repoRoot,
            IProgress<string>? progress = null, CancellationToken ct = default,
            string? initUrl = null, string initRemoteName = "origin");

        /// <summary>
        /// 读取 &lt;dir&gt;/.git/config 中指定 remote 的 URL。返回 (RemoteName, Url, Logs)。
        /// 找不到段/段内 url 时 RemoteName=parameter, Url=null。
        /// </summary>
        Task<(string RemoteName, string? Url, List<string> Logs)> DetectRemoteAsync(
            string dir, string remoteName = "origin", CancellationToken ct = default);

        /// <summary>
        /// 把指定目录确保成为绑定到 (url, remoteName) 的 git 仓库:
        /// 1) initIfMissing 且 &lt;dir&gt; 缺 .git → 执行 <c>git init</c>
        /// 2) 已存在 remote 时执行 <c>git remote set-url</c>;否则执行 <c>git remote add</c>
        /// 3) 当 url 为 null/空时,只做 1) 不动 remote。
        /// </summary>
        Task<CloneResult> EnsureRemoteAsync(
            string dir, string? url, string remoteName = "origin",
            bool initIfMissing = true, IProgress<string>? progress = null,
            CancellationToken ct = default);
    }
}