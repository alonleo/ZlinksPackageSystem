using System;
using System.IO;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 纯函数：从 Git URL 解析仓库名 / 计算仓库根路径。
    /// 不做 I/O，便于单测。
    /// </summary>
    public static class GitUrlParser
    {
        public static string ParseRepoName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Git URL 不能为空", nameof(url));

            var s = url.Trim();
            if (s.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                s = s[..^4];
            if (s.EndsWith("/", StringComparison.Ordinal))
                s = s[..^1];

            int lastSlash = s.LastIndexOf('/');
            int lastColon = s.LastIndexOf(':');
            int start = Math.Max(lastSlash, lastColon) + 1;
            if (start >= s.Length)
                throw new ArgumentException($"无法解析仓库名：{url}", nameof(url));

            var name = s[start..];
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"无法解析仓库名：{url}", nameof(url));

            return name;
        }

        public static string CombineRepoRoot(string targetParentDir, string repoName)
        {
            if (string.IsNullOrEmpty(targetParentDir))
                throw new ArgumentException("targetParentDir 不能为空", nameof(targetParentDir));
            if (string.IsNullOrEmpty(repoName))
                throw new ArgumentException("repoName 不能为空", nameof(repoName));
            return Path.Combine(targetParentDir, repoName);
        }
    }
}