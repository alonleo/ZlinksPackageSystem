using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface IGlobalNotificationService
    {
        string DefaultFilePath { get; }
        Task<GlobalNotificationConfig> LoadAsync(CancellationToken ct = default);
        Task SaveAsync(GlobalNotificationConfig config, CancellationToken ct = default);
    }
}