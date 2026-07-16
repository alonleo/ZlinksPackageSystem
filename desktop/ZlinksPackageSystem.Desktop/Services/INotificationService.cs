using System.Threading;
using System.Threading.Tasks;
using ZlinksPackageSystem.Desktop.Models;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface INotificationService
    {
        Task<System.Collections.Generic.List<NotificationSendResult>> SendAsync(
            ToolProject project,
            ToolRunSnapshot snapshot,
            CancellationToken ct = default);
    }
}