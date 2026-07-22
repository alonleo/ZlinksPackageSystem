using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ZlinksPackageSystem.Desktop.Services
{
    public class NetworkStatusService : INetworkStatusService, IDisposable
    {
        private readonly IApiService _apiService;
        private readonly HttpClient _pingClient;
        private readonly int _timeoutMs;
        private readonly int _pollIntervalMs;
        private readonly string _healthEndpoint;

        private CancellationTokenSource? _monitorCts;
        private Task? _monitorTask;
        private readonly object _stateLock = new();
        private bool _isOnline = true;
        private bool _isLocalMode;
        private bool _disposed;

        public event EventHandler<bool>? StatusChanged;

        public bool IsOnline
        {
            get { lock (_stateLock) return _isOnline; }
            private set
            {
                bool changed;
                lock (_stateLock)
                {
                    if (_isOnline == value) return;
                    _isOnline = value;
                    changed = true;
                }
                if (changed)
                {
                    StatusChanged?.Invoke(this, value);
                }
            }
        }

        public bool IsLocalMode
        {
            get { lock (_stateLock) return _isLocalMode; }
            private set
            {
                bool changed;
                lock (_stateLock)
                {
                    if (_isLocalMode == value) return;
                    _isLocalMode = value;
                    changed = true;
                }
                if (changed)
                {
                    StatusChanged?.Invoke(this, IsOnline);
                }
            }
        }

        public NetworkStatusService(IConfiguration configuration, IApiService apiService)
        {
            _apiService = apiService;
            _timeoutMs = int.TryParse(configuration["NetworkCheckTimeoutMs"], out var t) && t > 0 ? t : 3000;
            _pollIntervalMs = int.TryParse(configuration["NetworkPollIntervalMs"], out var p) && p > 0 ? p : 30000;
            _healthEndpoint = (configuration["NetworkHealthEndpoint"] ?? "/health").TrimStart('/');

            _pingClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(_timeoutMs)
            };
            _pingClient.DefaultRequestHeaders.Add("User-Agent", "ZlinksPackageSystem-Desktop/1.0");

            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        public async Task<bool> CheckConnectivityAsync()
        {
            var networkAvailable = NetworkInterface.GetIsNetworkAvailable();

            var baseUri = _apiService.BaseUrl.TrimEnd('/');
            var healthUrl = $"{baseUri}/{_healthEndpoint}";

            try
            {
                using var cts = new CancellationTokenSource(_timeoutMs);
                using var request = new HttpRequestMessage(HttpMethod.Get, healthUrl);
                using var response = await _pingClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                var ok = response.IsSuccessStatusCode;
                SetOnline(ok, false);
                return ok;
            }
            catch
            {
                try
                {
                    using var cts = new CancellationTokenSource(_timeoutMs);
                    var baseCheckUrl = $"{baseUri}/auth/info";
                    using var request = new HttpRequestMessage(HttpMethod.Get, baseCheckUrl);
                    using var response = await _pingClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    var ok = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized;
                    SetOnline(ok, false);
                    return ok;
                }
                catch
                {
                    SetOnline(false, networkAvailable);
                    return false;
                }
            }
        }

        public void StartMonitoring()
        {
            if (_disposed) return;
            lock (_stateLock)
            {
                if (_monitorCts != null) return;
                _monitorCts = new CancellationTokenSource();
            }

            var token = _monitorCts!.Token;
            _monitorTask = Task.Run(async () =>
            {
                try
                {
                    await CheckConnectivityAsync();
                }
                catch
                {
                }

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_pollIntervalMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    if (token.IsCancellationRequested) break;

                    try
                    {
                        await CheckConnectivityAsync();
                    }
                    catch
                    {
                    }
                }
            }, token);
        }

        public void StopMonitoring()
        {
            CancellationTokenSource? cts;
            lock (_stateLock)
            {
                cts = _monitorCts;
                _monitorCts = null;
            }
            cts?.Cancel();
            cts?.Dispose();
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckConnectivityAsync();
                }
                catch
                {
                }
            });
        }

        private void SetOnline(bool online, bool isLocal)
        {
            lock (_stateLock)
            {
                var changed = (_isOnline != online) || (_isLocalMode != isLocal);
                _isOnline = online;
                _isLocalMode = isLocal;
                if (changed)
                {
                    StatusChanged?.Invoke(this, online);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopMonitoring();
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            _pingClient.Dispose();
        }
    }
}
