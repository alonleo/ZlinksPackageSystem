using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        private readonly int _failoverMs;
        private readonly int _debounceMs;
        private readonly string _healthEndpoint;

        private CancellationTokenSource? _monitorCts;
        private Task? _monitorTask;
        private readonly object _stateLock = new();
        private bool _isOnline = true;
        private bool _isLocalMode;
        private bool _hasInitialResult;
        private bool _disposed;

        private readonly SemaphoreSlim _checkGate = new(1, 1);
        private long _latestRequestId;

        private readonly Timer _adapterDebounceTimer;
        private long _adapterDebounceRequestId;
        private readonly object _adapterDebounceLock = new();

        private readonly LinkedList<NetworkTransitionRecord> _recent = new();
        private const int MaxRecent = 50;

        public event EventHandler<bool>? StatusChanged;

        public bool IsOnline
        {
            get { lock (_stateLock) return _isOnline; }
        }

        public bool IsLocalMode
        {
            get { lock (_stateLock) return _isLocalMode; }
        }

        public bool HasInitialResult
        {
            get { lock (_stateLock) return _hasInitialResult; }
        }

        public NetworkStatusService(IConfiguration configuration, IApiService apiService)
        {
            _apiService = apiService;
            _timeoutMs = int.TryParse(configuration["NetworkCheckTimeoutMs"], out var t) && t > 0 ? t : 5000;
            _pollIntervalMs = int.TryParse(configuration["NetworkPollIntervalMs"], out var p) && p > 0 ? p : 30000;
            _failoverMs = int.TryParse(configuration["NetworkFailoverMs"], out var f) && f > 0 ? f : 2000;
            _debounceMs = int.TryParse(configuration["NetworkDebounceMs"], out var d) && d > 0 ? d : 1500;
            _healthEndpoint = (configuration["NetworkHealthEndpoint"] ?? "/health").TrimStart('/');

            _pingClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(_timeoutMs)
            };
            _pingClient.DefaultRequestHeaders.Add("User-Agent", "ZlinksPackageSystem-Desktop/1.0");

            _adapterDebounceTimer = new Timer(OnAdapterDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        public async Task<bool> CheckConnectivityAsync(string reason = "manual")
        {
            if (_disposed) return false;

            var myId = Interlocked.Increment(ref _latestRequestId);

            await _checkGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (myId != Interlocked.Read(ref _latestRequestId))
                {
                    return IsOnline;
                }

                var online = await ProbeAsync(myId, reason).ConfigureAwait(false);

                if (myId != Interlocked.Read(ref _latestRequestId))
                {
                    return IsOnline;
                }

                return online;
            }
            finally
            {
                _checkGate.Release();
            }
        }

        private async Task<bool> ProbeAsync(long requestId, string reason)
        {
            var networkAvailable = NetworkInterface.GetIsNetworkAvailable();
            var baseUri = _apiService.BaseUrl.TrimEnd('/');
            var healthUrl = $"{baseUri}/{_healthEndpoint}";
            var authUrl = $"{baseUri}/auth/info";

            var primaryResult = await TryProbeAsync(healthUrl, requestId).ConfigureAwait(false);

            if (myIdMatches(requestId) && primaryResult.TransportOnline)
            {
                SetOnline(true, false, requestId, reason);
                return true;
            }

            if (myIdMatches(requestId) && primaryResult.RespondedWithServerError)
            {
                SetOnline(true, false, requestId, reason + ":5xx");
                return true;
            }

            if (myIdMatches(requestId) && primaryResult.Responded)
            {
                SetOnline(true, false, requestId, reason);
                return true;
            }

            var secondaryResult = await TryProbeAsync(authUrl, requestId).ConfigureAwait(false);

            if (myIdMatches(requestId) && (secondaryResult.TransportOnline || secondaryResult.RespondedWithServerError))
            {
                SetOnline(true, false, requestId, reason + ":auth");
                return true;
            }

            if (myIdMatches(requestId) && secondaryResult.Responded)
            {
                SetOnline(true, false, requestId, reason + ":auth");
                return true;
            }

            var adapterOffline = !networkAvailable;
            var isLocal = !adapterOffline;

            if (myIdMatches(requestId))
            {
                SetOnline(false, isLocal, requestId, reason + ":fail");
                return false;
            }

            return IsOnline;
        }

        private bool myIdMatches(long requestId)
        {
            return Interlocked.Read(ref _latestRequestId) == requestId;
        }

        private readonly struct ProbeResult
        {
            public ProbeResult(bool transportOnline, bool responded, bool respondedWithServerError)
            {
                TransportOnline = transportOnline;
                Responded = responded;
                RespondedWithServerError = respondedWithServerError;
            }

            public bool TransportOnline { get; }
            public bool Responded { get; }
            public bool RespondedWithServerError { get; }

            public static ProbeResult None { get; } = new(false, false, false);
        }

        private async Task<ProbeResult> TryProbeAsync(string url, long requestId)
        {
            try
            {
                using var cts = new CancellationTokenSource(_timeoutMs);
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await _pingClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return new ProbeResult(transportOnline: true, responded: true, respondedWithServerError: false);
                }

                var status = (int)response.StatusCode;
                if (status >= 500 && status <= 599)
                {
                    return new ProbeResult(transportOnline: false, responded: true, respondedWithServerError: true);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ProbeResult(transportOnline: false, responded: true, respondedWithServerError: false);
                }

                return ProbeResult.None;
            }
            catch (HttpRequestException ex) when (IsTransportFailure(ex))
            {
                return ProbeResult.None;
            }
            catch (TaskCanceledException)
            {
                return ProbeResult.None;
            }
            catch (OperationCanceledException)
            {
                return ProbeResult.None;
            }
            catch
            {
                return ProbeResult.None;
            }
        }

        private static bool IsTransportFailure(HttpRequestException ex)
        {
            var inner = ex.InnerException;
            return inner is SocketException ||
                   inner is IOException ||
                   ex.StatusCode == null;
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
                    await CheckConnectivityAsync("startup").ConfigureAwait(false);
                }
                catch
                {
                }

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_pollIntervalMs, token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    if (token.IsCancellationRequested) break;

                    try
                    {
                        await CheckConnectivityAsync("poll").ConfigureAwait(false);
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
            lock (_adapterDebounceLock)
            {
                Interlocked.Increment(ref _adapterDebounceRequestId);
                _adapterDebounceTimer.Change(_debounceMs, Timeout.Infinite);
            }
        }

        private void OnAdapterDebounceElapsed(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckConnectivityAsync("adapter-debounce").ConfigureAwait(false);
                }
                catch
                {
                }
            });
        }

        private void SetOnline(bool online, bool isLocal, long requestId, string reason)
        {
            bool shouldRaise = false;
            lock (_stateLock)
            {
                if (requestId != Interlocked.Read(ref _latestRequestId)) return;

                var changed = (_isOnline != online) || (_isLocalMode != isLocal);
                var firstResult = !_hasInitialResult;

                _isOnline = online;
                _isLocalMode = isLocal;
                _hasInitialResult = true;

                if (changed || firstResult)
                {
                    shouldRaise = true;
                }
            }

            if (shouldRaise)
            {
                RecordTransition(online, isLocal, requestId, reason);
                StatusChanged?.Invoke(this, online);
            }
        }

        private void RecordTransition(bool online, bool isLocal, long requestId, string reason)
        {
            var rec = new NetworkTransitionRecord(DateTime.UtcNow, online, isLocal, requestId, reason);
            lock (_recent)
            {
                _recent.AddFirst(rec);
                while (_recent.Count > MaxRecent)
                {
                    _recent.RemoveLast();
                }
            }
        }

        public IReadOnlyList<NetworkTransitionRecord> GetRecentTransitions()
        {
            lock (_recent)
            {
                return _recent.ToArray();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopMonitoring();
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            _adapterDebounceTimer.Dispose();
            _pingClient.Dispose();
            _checkGate.Dispose();
        }
    }
}
