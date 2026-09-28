using Microsoft.AspNetCore.SignalR.Client;


namespace FleetDashboard.Client.Streaming;


/// <summary>Narrow transport surface over <see cref="HubConnection"/>, so the snapshot, reconnect,
/// and connect-backoff paths can be exercised without a live fleet service.</summary>
public interface IFleetHubConnection : IAsyncDisposable
{
    /// <summary>Current transport state; a snapshot request is only valid while connected.</summary>
    HubConnectionState State { get; }

    /// <summary>Registers a handler for a server-to-client method.</summary>
    IDisposable On<T>(string methodName, Func<T, Task> handler);

    /// <summary>Starts the connection, throwing when the transport cannot be established.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Invokes a hub method that returns a value.</summary>
    Task<T> InvokeAsync<T>(string methodName, CancellationToken cancellationToken = default);

    /// <summary>Invokes a hub method that returns no value.</summary>
    Task SendAsync(string methodName, CancellationToken cancellationToken = default);

    /// <summary>Invokes a hub method with one argument.</summary>
    Task SendAsync(string methodName, object? arg1, CancellationToken cancellationToken = default);

    /// <summary>Raised when the transport begins an automatic reconnect.</summary>
    event Func<Exception?, Task>? Reconnecting;

    /// <summary>Raised when an automatic reconnect restores the connection.</summary>
    event Func<string?, Task>? Reconnected;

    /// <summary>Raised when the transport closes for good.</summary>
    event Func<Exception?, Task>? Closed;
}
