using System;
using System.IO;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 纯函数：从 Git URL 解析仓库名 / 计算仓库根路径 / 解析 .git/config 的 [remote] url。
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
            // Path.Combine 在 Windows 上会把前导 `/` 重写成 `\`,破坏 Unix 风格根路径。
            // 若传入的是绝对 Unix 路径(以 `/` 开头),用手动拼接保留前导分隔符,避免跨平台运行时出错。
            if (targetParentDir.StartsWith('/'))
                return "/" + targetParentDir.Trim('/').TrimEnd('/') + "/" + repoName;
            return Path.Combine(targetParentDir, repoName);
        }

        /// <summary>
        /// 从 .git/config 文本中解析指定远程名(默认 origin)的 url。
        /// 兼容带引号 / 不带引号 / 含 [remote "name"] 任意大小写 / 含前置 # 注释。
        /// 找不到对应段或 url 时返回 null(不抛异常,便于 SMB/CI 场景)。
        /// </summary>
        public static string? ParseRemoteUrl(string gitConfigText, string remoteName = "origin")
        {
            if (string.IsNullOrWhiteSpace(gitConfigText)) return null;
            if (string.IsNullOrWhiteSpace(remoteName)) remoteName = "origin";

            // 段头匹配:[remote "xxx"]  或  [remote xxx]  (Git 标准使用前者)
            string sectionHeader = $"[remote \"{remoteName}\"]";
            int idx = gitConfigText.IndexOf(sectionHeader, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            // 定位到段尾(下一个 [xxx] 段或文件末尾)
            int sectionStart = idx + sectionHeader.Length;
            int nextBracket = gitConfigText.IndexOf('\n', sectionStart);
            if (nextBracket < 0) nextBracket = gitConfigText.Length;
            int sectionEnd = gitConfigText.IndexOf("\n[", nextBracket, StringComparison.Ordinal);
            if (sectionEnd < 0) sectionEnd = gitConfigText.Length;
            string section = gitConfigText.Substring(sectionStart, sectionEnd - sectionStart);

            // 在段内找 url = ...(支持带引号)
            foreach (var rawLine in section.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                    continue;
                // 兼容 \t 和空格分隔
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                if (!string.Equals(key, "url", StringComparison.OrdinalIgnoreCase))
                    continue;
                var val = line.Substring(eq + 1).Trim();
                // 去掉包裹的双引号(常见于 git 配置里的复杂 URL)
                if (val.Length >= 2 && val[0] == '"' && val[^1] == '"')
                    val = val.Substring(1, val.Length - 2);
                return string.IsNullOrEmpty(val) ? null : val;
            }
            return null;
        }
    }
}