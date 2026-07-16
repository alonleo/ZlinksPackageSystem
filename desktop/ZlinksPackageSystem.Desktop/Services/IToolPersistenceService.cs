using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IToolPersistenceService
    {
        string DefaultFilePath { get; }
        Task<List<ToolProject>> LoadAsync(CancellationToken ct = default);
        Task SaveAsync(IEnumerable<ToolProject> projects, CancellationToken ct = default);
        Task ExportAsync(string filePath, IEnumerable<ToolProject> projects, CancellationToken ct = default);
        Task<List<ToolProject>?> ImportAsync(string filePath, CancellationToken ct = default);
    }
}