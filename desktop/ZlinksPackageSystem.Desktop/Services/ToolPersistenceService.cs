using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class ToolPersistenceService : IToolPersistenceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _directory;
        private readonly string _defaultFile;

        public ToolPersistenceService(string? directoryOverride = null)
        {
            _directory = directoryOverride
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ZlinksPackageSystem");
            _defaultFile = Path.Combine(_directory, "tools.json");
        }

        public string DefaultFilePath => _defaultFile;

        public async Task<List<ToolProject>> LoadAsync(CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(_defaultFile)) return new List<ToolProject>();
                await using var fs = File.OpenRead(_defaultFile);
                var list = await JsonSerializer.DeserializeAsync<List<ToolProject>>(fs, JsonOptions, ct);
                return list ?? new List<ToolProject>();
            }
            catch
            {
                return new List<ToolProject>();
            }
        }

        public async Task SaveAsync(IEnumerable<ToolProject> projects, CancellationToken ct = default)
        {
            Directory.CreateDirectory(_directory);
            var list = new List<ToolProject>(projects);
            await using var fs = File.Create(_defaultFile);
            await JsonSerializer.SerializeAsync(fs, list, JsonOptions, ct);
        }

        public async Task ExportAsync(string filePath, IEnumerable<ToolProject> projects, CancellationToken ct = default)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var list = new List<ToolProject>(projects);
            await using var fs = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fs, list, JsonOptions, ct);
        }

        public async Task<List<ToolProject>?> ImportAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                await using var fs = File.OpenRead(filePath);
                var list = await JsonSerializer.DeserializeAsync<List<ToolProject>>(fs, JsonOptions, ct);
                return list ?? new List<ToolProject>();
            }
            catch
            {
                return null;
            }
        }
    }
}