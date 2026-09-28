namespace FleetDashboard.Client.Streaming;


/// <summary>Transport-level connection state a frame source can surface to the dashboard, so an
/// automatic reconnect is visible instead of masquerading as live telemetry.</summary>
public interface IFleetConnectionStatus
{
    /// <summary>True while the client is attempting to re-establish a dropped connection.</summary>
    bool IsReconnecting { get; }

    /// <summary>How many times automatic reconnect has restored the connection this session.</summary>
    int ReconnectCount { get; }

    /// <summary>Clears any connect backoff so the next frame request attempts a connection now.</summary>
    Task RetryConnectionAsync(CancellationToken cancellationToken = default);
}
