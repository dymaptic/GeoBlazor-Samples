using System.Collections.Concurrent;

namespace FleetDashboard.Server.Streaming;

/// <summary>Viewport filters for the connections that registered one. Connections absent from
/// this registry receive the full broadcast frame.</summary>
public sealed class ViewportSubscriptions
{
    private readonly ConcurrentDictionary<string, ViewportFrameFilter> _filters = new();

    public ViewportFrameFilter GetOrAdd(string connectionId) => _filters.GetOrAdd(connectionId, _ => new());

    public void Remove(string connectionId) => _filters.TryRemove(connectionId, out _);

    public IReadOnlyList<KeyValuePair<string, ViewportFrameFilter>> Snapshot() => _filters.ToArray();
}
