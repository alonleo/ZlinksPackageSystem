using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZlinksPackageSystem.Desktop.Services
{
    public interface INetworkStatusService
    {
        bool IsOnline { get; }

        bool IsLocalMode { get; }

        bool HasInitialResult { get; }

        event EventHandler<bool>? StatusChanged;

        Task<bool> CheckConnectivityAsync(string reason = "manual");

        void StartMonitoring();

        void StopMonitoring();

        IReadOnlyList<NetworkTransitionRecord> GetRecentTransitions();
    }

    public record NetworkTransitionRecord(
        DateTime AtUtc,
        bool Online,
        bool LocalMode,
        long RequestId,
        string Reason);
}
