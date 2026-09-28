using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace FleetDashboard.Client.Streaming;

public sealed class SignalRFleetFrameSource
    : IFleetFrameSource, IFleetScenarioControls, IFleetConnectionStatus, IAsyncDisposable
{
    /// <summary>Minimum gap between automatic connection attempts after an initial failure, so a
    /// 250 ms update loop cannot hammer an unavailable server. A manual retry clears it.</summary>
    private static readonly TimeSpan ConnectRetryInterval = TimeSpan.FromSeconds(2);

    /// <summary>Minimum time the "Reconnecting" state stays visible after a drop so the audience
    /// can see it before recovery; matches the first automatic-reconnect delay.</summary>
    private static readonly TimeSpan ReconnectVisibleDelay = TimeSpan.FromSeconds(1);

    public SignalRFleetFrameSource(IFleetHubConnection connection, TimeProvider clock)
    {
        _connection = connection;
        _clock = clock;
        _connection.On<FleetFrame>("ReceiveFleetFrame", ReceiveLiveFrame);
        _connection.Reconnecting += _ => { SetReconnecting(true); return Task.CompletedTask; };
        _connection.Reconnected += _ =>
        {
            lock (_sync)
            {
                _reconnecting = false;
                _reconnectStartedAtUtc = null;
                _reconnectCount++;
            }

            return MarkSnapshotRequiredAsync();
        };
        _connection.Closed += _ =>
        {
            SetReconnecting(false);
            return MarkSnapshotRequiredAsync();
        };
    }

    public bool IsReconnecting
    {
        get { lock (_sync) return _reconnecting; }
    }

    public int ReconnectCount
    {
        get { lock (_sync) return _reconnectCount; }
    }

    public async ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                bool wasConnected;
                lock (_sync) wasConnected = _everConnected;

                if (wasConnected)
                {
                    if (!await ReconnectAsync(cancellationToken)) return GetCurrentFrame();
                }
                else if (!ShouldAttemptConnect())
                {
                    throw new InvalidOperationException("Still trying to reach the fleet service.");
                }
                else
                {
                    try
                    {
                        await _connection.StartAsync(cancellationToken);
                        lock (_sync)
                        {
                            _lastConnectFailedAtUtc = null;
                            _everConnected = true;
                        }
                        MarkSnapshotRequired();
                    }
                    catch (Exception) when (!cancellationToken.IsCancellationRequested)
                    {
                        lock (_sync) _lastConnectFailedAtUtc = _clock.GetUtcNow();
                        throw;
                    }
                }
            }

            // A snapshot request is only meaningful on a live transport: during the reconnect
            // window the state can be Connecting, and invoking there would throw instead of
            // serving the last known frame while the reconnect path re-establishes the connection.
            if (IsSnapshotRequired() && _connection.State == HubConnectionState.Connected)
            {
                FleetFrame snapshot = await _connection.InvokeAsync<FleetFrame>("GetSnapshot", cancellationToken);
                AcceptSnapshot(snapshot);
            }

            return GetCurrentFrame();
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    /// <summary>A dropped connection holds "Reconnecting" for a visible beat before re-establishing.
    /// The server abort closes cleanly, which the transport treats as a normal close that
    /// <c>WithAutomaticReconnect</c> does not recover from, so this path owns the visible recovery.
    /// Returns false while still waiting, so the caller can serve the last-known frame.</summary>
    private async Task<bool> ReconnectAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt;
        lock (_sync)
        {
            if (!_reconnecting)
            {
                _reconnecting = true;
                _reconnectStartedAtUtc = _clock.GetUtcNow();
            }

            startedAt = _reconnectStartedAtUtc ?? _clock.GetUtcNow();
        }

        if (_clock.GetUtcNow() - startedAt < ReconnectVisibleDelay) return false;

        try
        {
            await _connection.StartAsync(cancellationToken);
            lock (_sync)
            {
                _reconnecting = false;
                _reconnectStartedAtUtc = null;
                _lastConnectFailedAtUtc = null;
                _everConnected = true;
                _reconnectCount++;
            }
            MarkSnapshotRequired();
            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            lock (_sync)
            {
                _reconnecting = false;
                _reconnectStartedAtUtc = null;
                _lastConnectFailedAtUtc = _clock.GetUtcNow();
            }
            throw;
        }
    }

    public Task RetryConnectionAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) _lastConnectFailedAtUtc = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        _connectionGate.Dispose();
    }

    public Task PauseVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
        _connection.SendAsync("PauseVehicle", vehicleId, cancellationToken);

    public Task ResumeVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
        _connection.SendAsync("ResumeVehicle", vehicleId, cancellationToken);

    public Task PauseFleetAsync(CancellationToken cancellationToken = default) =>
        _connection.SendAsync("PauseFleet", cancellationToken);

    public Task ResumeFleetAsync(CancellationToken cancellationToken = default) =>
        _connection.SendAsync("ResumeFleet", cancellationToken);

    public Task ResetAsync(CancellationToken cancellationToken = default) =>
        _connection.SendAsync("Reset", cancellationToken);

    public Task SetPresetAsync(string presetName, CancellationToken cancellationToken = default) =>
        _connection.SendAsync("SetPreset", presetName, cancellationToken);

    public async Task BreakConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connection.SendAsync("BreakConnection", cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // The server aborts the transport as part of the command, so the send may not complete.
        }
    }

    private bool ShouldAttemptConnect()
    {
        lock (_sync)
        {
            DateTimeOffset? failedAt = _lastConnectFailedAtUtc;
            return failedAt is null || _clock.GetUtcNow() - failedAt.Value >= ConnectRetryInterval;
        }
    }

    private void SetReconnecting(bool value)
    {
        lock (_sync)
        {
            _reconnecting = value;
            _reconnectStartedAtUtc = value ? _clock.GetUtcNow() : null;
        }
    }

    private Task ReceiveLiveFrame(FleetFrame frame)
    {
        lock (_sync)
        {
            if (_activeRunId == frame.RunId)
            {
                _currentFrame = _currentFrame is null ? frame : FleetFrameOrdering.Newest(_currentFrame, frame);
            }
            else
            {
                _pendingLiveFrame = _pendingLiveFrame is null ? frame
                    : FleetFrameOrdering.Newest(_pendingLiveFrame, frame);
                _snapshotRequired = true;
            }
        }

        return Task.CompletedTask;
    }

    private Task MarkSnapshotRequiredAsync()
    {
        MarkSnapshotRequired();
        return Task.CompletedTask;
    }

    private void MarkSnapshotRequired()
    {
        lock (_sync)
        {
            _snapshotRequired = true;
        }
    }

    private bool IsSnapshotRequired()
    {
        lock (_sync)
        {
            return _snapshotRequired;
        }
    }

    private FleetFrame GetCurrentFrame()
    {
        lock (_sync)
        {
            return _currentFrame ?? throw new InvalidOperationException("The fleet snapshot was empty.");
        }
    }

    private void AcceptSnapshot(FleetFrame snapshot)
    {
        lock (_sync)
        {
            _activeRunId = snapshot.RunId;
            _currentFrame = FleetFrameOrdering.ReconcileSnapshot(snapshot, _currentFrame, _pendingLiveFrame);
            _pendingLiveFrame = null;
            _snapshotRequired = false;
        }
    }

    private readonly IFleetHubConnection _connection;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly object _sync = new();
    private Guid? _activeRunId;
    private FleetFrame? _currentFrame;
    private FleetFrame? _pendingLiveFrame;
    private DateTimeOffset? _lastConnectFailedAtUtc;
    private DateTimeOffset? _reconnectStartedAtUtc;
    private bool _everConnected;
    private bool _reconnecting;
    private int _reconnectCount;
    private bool _snapshotRequired = true;
}
