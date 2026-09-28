using Microsoft.AspNetCore.SignalR.Client;


namespace FleetDashboard.Client.Streaming;


/// <summary>Adapts a SignalR <see cref="HubConnection"/> to <see cref="IFleetHubConnection"/> for
/// the dashboard's frame source.</summary>
public sealed class HubConnectionAdapter(HubConnection connection) : IFleetHubConnection
{
    public HubConnectionState State => connection.State;

    public event Func<Exception?, Task>? Reconnecting
    {
        add => connection.Reconnecting += value;
        remove => connection.Reconnecting -= value;
    }

    public event Func<string?, Task>? Reconnected
    {
        add => connection.Reconnected += value;
        remove => connection.Reconnected -= value;
    }

    public event Func<Exception?, Task>? Closed
    {
        add => connection.Closed += value;
        remove => connection.Closed -= value;
    }

    public IDisposable On<T>(string methodName, Func<T, Task> handler) => connection.On(methodName, handler);

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        connection.StartAsync(cancellationToken);

    public Task<T> InvokeAsync<T>(string methodName, CancellationToken cancellationToken = default) =>
        connection.InvokeAsync<T>(methodName, cancellationToken);

    public Task SendAsync(string methodName, CancellationToken cancellationToken = default) =>
        connection.SendAsync(methodName, cancellationToken);

    public Task SendAsync(string methodName, object? arg1, CancellationToken cancellationToken = default) =>
        connection.SendAsync(methodName, arg1, cancellationToken);

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}
