using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    /// <summary>
    /// 飞书通知发送服务（Q3-C 混合模式）
    /// </summary>
    public class NotificationService : INotificationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions CardJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly IGlobalNotificationService _global;
        private readonly HttpClient _http;
        private readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(5);

        // 应用机器人 token 缓存（单进程，无需线程安全：UI 单线程触发）
        private string _appToken = string.Empty;
        private DateTime _appTokenExpiry = DateTime.MinValue;

        public NotificationService(IGlobalNotificationService global, HttpMessageHandler? handler = null)
        {
            _global = global;
            _http = new HttpClient(handler ?? new HttpClientHandler()) { Timeout = _httpTimeout };
        }

        public async Task<List<NotificationSendResult>> SendAsync(
            ToolProject project, ToolRunSnapshot snapshot, CancellationToken ct = default)
        {
            var results = new List<NotificationSendResult>();
            try
            {
                var global = await _global.LoadAsync(ct);
                if (!global.IsEnabled) return results;

                var (effectiveTrigger, effectiveMax, effectiveChannels) = ResolveEffective(project, global, snapshot.Trigger);

                if (!effectiveTrigger) return results;
                if (effectiveChannels.Count == 0) return results;

                // 截断输出
                var truncated = Truncate(snapshot.Output, effectiveMax);

                foreach (var ch in effectiveChannels)
                {
                    var result = await SendOneAsync(project, snapshot, truncated, ch, ct);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Notification] SendAsync 异常：{ex.Message}");
            }
            return results;
        }

        private (bool allowed, int maxChars, IList<FeishuConfig> channels) ResolveEffective(
            ToolProject project, GlobalNotificationConfig global, NotificationTrigger trigger)
        {
            var t = project.Notification ?? new NotificationConfig();
            if (t.UseGlobalSettings)
            {
                bool allowed = trigger switch
                {
                    NotificationTrigger.Start => global.NotifyOnStart,
                    NotificationTrigger.Success => global.NotifyOnSuccess,
                    NotificationTrigger.Failure => global.NotifyOnFailure,
                    _ => false
                };
                return (allowed, global.MaxOutputChars, global.Channels);
            }
            else
            {
                bool allowed = trigger switch
                {
                    NotificationTrigger.Start => t.NotifyOnStart,
                    NotificationTrigger.Success => t.NotifyOnSuccess,
                    NotificationTrigger.Failure => t.NotifyOnFailure,
                    _ => false
                };
                return (allowed, t.MaxOutputChars, t.Channels);
            }
        }

        private async Task<NotificationSendResult> SendOneAsync(
            ToolProject project, ToolRunSnapshot snapshot, string output, FeishuConfig channel, CancellationToken ct)
        {
            // 渠道级别分发：飞书 → 内部 Custom/App 分派；微信企业 → markdown 推送
            if (channel.ChannelType == ChannelType.WeChatWork)
            {
                return await SendWeChatWorkAsync(channel, project, snapshot, output, ct);
            }

            var label = channel.RobotType == FeishuRobotType.Custom ? "自定义机器人" : $"应用({channel.AppId})";
            try
            {
                var card = BuildCardObject(project, snapshot, output, channel);
                if (channel.RobotType == FeishuRobotType.Custom)
                {
                    return await SendCustomAsync(channel, card, label, ct);
                }
                else
                {
                    return await SendAppAsync(channel, card, label, ct);
                }
            }
            catch (Exception ex)
            {
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = ex.Message };
            }
        }

        private async Task<NotificationSendResult> SendCustomAsync(FeishuConfig ch, object card, string label, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ch.WebhookUrl))
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = "Webhook URL 为空" };

            var payload = new
            {
                msg_type = "interactive",
                card,
                at = new { is_at_all = ch.AtAll, at_mobiles = ch.AtMobiles ?? new List<string>() }
            };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var resp = await _http.PostAsync(ch.WebhookUrl, new StringContent(json, Encoding.UTF8, "application/json"), ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            return new NotificationSendResult
            {
                ChannelLabel = label,
                Success = resp.IsSuccessStatusCode && !body.Contains("\"code\":"),
                Message = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}"
            };
        }

        private async Task<NotificationSendResult> SendAppAsync(FeishuConfig ch, object card, string label, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ch.AppId) || string.IsNullOrWhiteSpace(ch.AppSecret))
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = "App ID 或 App Secret 为空" };
            if (string.IsNullOrWhiteSpace(ch.ReceiveId))
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = "ReceiveId 为空，跳过" };

            var token = await GetAppTokenAsync(ch, ct);
            if (string.IsNullOrEmpty(token))
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = "获取 tenant_access_token 失败" };

            var payload = new
            {
                receive_id = ch.ReceiveId,
                msg_type = "interactive",
                card,
                at = new { is_at_all = ch.AtAll, at_mobiles = ch.AtMobiles ?? new List<string>() }
            };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var req = new HttpRequestMessage(HttpMethod.Post,
                "https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=chat_id")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.Add("Authorization", $"Bearer {token}");
            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            return new NotificationSendResult
            {
                ChannelLabel = label,
                Success = resp.IsSuccessStatusCode && !body.Contains("\"code\":"),
                Message = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}"
            };
        }

        /// <summary>
        /// 微信企业群机器人推送（markdown 模式）。
        /// 企业微信 Webhook URL 已自带 key，POST body 为 markdown JSON。
        /// </summary>
        private async Task<NotificationSendResult> SendWeChatWorkAsync(
            FeishuConfig ch, ToolProject project, ToolRunSnapshot snapshot, string output, CancellationToken ct)
        {
            const string label = "微信企业机器人";
            if (string.IsNullOrWhiteSpace(ch.WebhookUrl))
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = "Webhook URL 为空" };

            var md = BuildWeChatMarkdownContent(project, snapshot, output);
            var mentionedList = new List<string>();
            var mentionedMobileList = new List<string>();
            if (ch.AtAll)
            {
                mentionedList.Add("@all");
            }
            if (ch.AtMobiles != null && ch.AtMobiles.Count > 0)
            {
                mentionedMobileList.AddRange(ch.AtMobiles);
            }

            var payload = new
            {
                msgtype = "markdown",
                markdown = new { content = md },
                mentioned_list = mentionedList,
                mentioned_mobile_list = mentionedMobileList
            };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            try
            {
                var resp = await _http.PostAsync(ch.WebhookUrl, new StringContent(json, Encoding.UTF8, "application/json"), ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                // 企业微信返回 {"errcode":0,"errmsg":"ok"}；errcode=0 才算成功
                bool ok = resp.IsSuccessStatusCode;
                if (ok)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("errcode", out var ec))
                        {
                            ok = ec.GetInt32() == 0;
                        }
                    }
                    catch { ok = resp.IsSuccessStatusCode; }
                }
                return new NotificationSendResult
                {
                    ChannelLabel = label,
                    Success = ok,
                    Message = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}"
                };
            }
            catch (Exception ex)
            {
                return new NotificationSendResult { ChannelLabel = label, Success = false, Message = ex.Message };
            }
        }

        private async Task<string> GetAppTokenAsync(FeishuConfig ch, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(_appToken) && DateTime.UtcNow < _appTokenExpiry)
                return _appToken;

            var payload = new { app_id = ch.AppId, app_secret = ch.AppSecret };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var resp = await _http.PostAsync(
                "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal",
                new StringContent(json, Encoding.UTF8, "application/json"), ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) return string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var code = doc.RootElement.TryGetProperty("code", out var c) ? c.GetInt32() : -1;
                if (code != 0) return string.Empty;
                _appToken = doc.RootElement.GetProperty("tenant_access_token").GetString() ?? string.Empty;
                var expire = doc.RootElement.TryGetProperty("expire", out var e) ? e.GetInt32() : 7200;
                _appTokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(60, expire - 120));
                return _appToken;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (max <= 0 || s == null) return string.Empty;
            if (s.Length <= max) return s;
            return "..." + s[^max..];
        }

        /// <summary>
        /// 构造飞书 interactive 卡片对象。
        /// 公开为 public static 便于单测。
        /// </summary>
        public static string BuildCardJson(ToolProject project, ToolRunSnapshot snapshot, FeishuConfig channel)
        {
            var output = Truncate(snapshot.Output ?? string.Empty, snapshot.Output?.Length ?? 0);
            var card = BuildCardObject(project, snapshot, output, channel);
            return JsonSerializer.Serialize(card, CardJsonOptions);
        }

        /// <summary>
        /// 为微信企业机器人构造 markdown 文本。
        /// 公开为 public static 便于单测验证内容。
        /// </summary>
        public static string BuildWeChatMarkdownContent(ToolProject project, ToolRunSnapshot snapshot, string output)
        {
            var (emoji, title) = snapshot.Trigger switch
            {
                NotificationTrigger.Start => ("▶", $"{project.Name} 启动"),
                NotificationTrigger.Success => ("✅", $"{project.Name} 成功"),
                NotificationTrigger.Failure => ("❌", $"{project.Name} 失败"),
                _ => ("📋", project.Name)
            };

            var statusText = snapshot.Trigger switch
            {
                NotificationTrigger.Start => "运行中",
                NotificationTrigger.Success => "退出码 0",
                NotificationTrigger.Failure => $"退出码 {snapshot.ExitCode}",
                _ => "-"
            };

            var sb = new StringBuilder();
            sb.AppendLine($"# {emoji} <font color=\"info\">{title}</font>");
            sb.AppendLine($"> **工具**: {project.Name}");
            sb.AppendLine($"> **状态**: {statusText}");
            sb.AppendLine($"> **耗时**: {snapshot.DurationMs} ms");
            sb.AppendLine($"> **PID**: {snapshot.ProcessId?.ToString() ?? "-"}");
            sb.AppendLine($"> **退出码**: {snapshot.ExitCode}");
            sb.AppendLine($"> **工作目录**: {snapshot.WorkingDirectory}");
            if (!string.IsNullOrEmpty(output))
            {
                sb.AppendLine();
                sb.AppendLine("**脚本输出（截断）**");
                sb.AppendLine("```");
                sb.AppendLine(output);
                sb.AppendLine("```");
            }
            sb.AppendLine();
            sb.AppendLine($"<sub>{snapshot.StartTime:yyyy-MM-dd HH:mm:ss} → {snapshot.EndTime:HH:mm:ss}</sub>");
            return sb.ToString();
        }

        private static object BuildCardObject(ToolProject project, ToolRunSnapshot snapshot, string output, FeishuConfig channel)
        {
            var (emoji, title, template) = snapshot.Trigger switch
            {
                NotificationTrigger.Start => ("▶", $"{project.Name} 启动", "blue"),
                NotificationTrigger.Success => ("✅", $"{project.Name} 成功", "green"),
                NotificationTrigger.Failure => ("❌", $"{project.Name} 失败", "red"),
                _ => ("📋", project.Name, "blue")
            };

            var header = new
            {
                title = new { tag = "plain_text", content = $"{emoji} {title}" },
                template
            };

            var statusText = snapshot.Trigger switch
            {
                NotificationTrigger.Start => "运行中",
                NotificationTrigger.Success => "退出码 0",
                NotificationTrigger.Failure => $"退出码 {snapshot.ExitCode}",
                _ => "-"
            };

            var fields = new[]
            {
                new { is_short = true, text = new { tag = "lark_md", content = $"**工具**\n{project.Name}" } },
                new { is_short = true, text = new { tag = "lark_md", content = $"**状态**\n{statusText}" } },
                new { is_short = true, text = new { tag = "lark_md", content = $"**耗时**\n{snapshot.DurationMs} ms" } },
                new { is_short = true, text = new { tag = "lark_md", content = $"**PID**\n{snapshot.ProcessId?.ToString() ?? "-"}" } },
                new { is_short = true, text = new { tag = "lark_md", content = $"**退出码**\n{snapshot.ExitCode}" } },
                new { is_short = true, text = new { tag = "lark_md", content = $"**工作目录**\n{snapshot.WorkingDirectory}" } }
            };

            var elements = new List<object>
            {
                new { tag = "div", fields },
                new { tag = "hr" }
            };

            if (!string.IsNullOrEmpty(output))
            {
                elements.Add(new { tag = "div", text = new { tag = "lark_md", content = $"**脚本输出（截断）**\n```\n{output}\n```" } });
                elements.Add(new { tag = "hr" });
            }

            elements.Add(new
            {
                tag = "note",
                elements = new[]
                {
                    new { tag = "plain_text", content = $"{snapshot.StartTime:yyyy-MM-dd HH:mm:ss} → {snapshot.EndTime:HH:mm:ss}" }
                }
            });

            return new { header, elements };
        }
    }
}