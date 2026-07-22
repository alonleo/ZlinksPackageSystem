using System;
using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface INetworkStatusService
    {
        bool IsOnline { get; }

        bool IsLocalMode { get; }

        event EventHandler<bool>? StatusChanged;

        Task<bool> CheckConnectivityAsync();

        void StartMonitoring();

        void StopMonitoring();
    }
}
