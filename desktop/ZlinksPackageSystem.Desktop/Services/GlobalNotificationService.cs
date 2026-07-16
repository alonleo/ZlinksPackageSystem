using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class GlobalNotificationService : IGlobalNotificationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _directory;
        private readonly string _defaultFile;

        public GlobalNotificationService(string? directoryOverride = null)
        {
            _directory = directoryOverride
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZlinksPackageSystem");
            _defaultFile = Path.Combine(_directory, "notification.json");
        }

        public string DefaultFilePath => _defaultFile;

        public async Task<GlobalNotificationConfig> LoadAsync(CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(_defaultFile)) return new GlobalNotificationConfig();
                await using var fs = File.OpenRead(_defaultFile);
                var c = await JsonSerializer.DeserializeAsync<GlobalNotificationConfig>(fs, JsonOptions, ct);
                return c ?? new GlobalNotificationConfig();
            }
            catch
            {
                return new GlobalNotificationConfig();
            }
        }

        public async Task SaveAsync(GlobalNotificationConfig config, CancellationToken ct = default)
        {
            Directory.CreateDirectory(_directory);
            await using var fs = File.Create(_defaultFile);
            await JsonSerializer.SerializeAsync(fs, config, JsonOptions, ct);
        }
    }
}