using FleetDashboard.Client.Rendering;
using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class ViewportMergeTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-09T12:00:00Z");

    [TestMethod]
    public void APartialFrameUpdatesTheRosterInsteadOfReplacingIt()
    {
        Guid runId = Guid.NewGuid();
        FleetFrame full = Frame(runId, 1, [1, 2, 3]);
        FleetFrame partial = Frame(runId, 2, [2]) with { IsPartial = true };

        FleetFrame merged = FleetFrameOrdering.Newest(full, partial);

        Assert.IsFalse(merged.IsPartial);
        Assert.AreEqual(2, merged.FrameSequence);
        CollectionAssert.AreEqual(new long[] { 1, 2, 1 }, merged.Vehicles.Select(v => v.Sequence).ToArray());
    }

    [TestMethod]
    public void ASnapshotThenANewerPartialFrameKeepsTheWholeRoster()
    {
        Guid runId = Guid.NewGuid();
        FleetFrame snapshot = Frame(runId, 4, [1, 2, 3]);
        FleetFrame pending = Frame(runId, 9, [3]) with { IsPartial = true };

        FleetFrame reconciled = FleetFrameOrdering.ReconcileSnapshot(snapshot, null, pending);

        Assert.AreEqual(9, reconciled.FrameSequence);
        Assert.IsFalse(reconciled.IsPartial);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, reconciled.Vehicles.Select(v => v.VehicleId).ToArray());
        Assert.AreEqual(9, reconciled.Vehicles.Single(v => v.VehicleId == 3).Sequence);
    }

    [TestMethod]
    public void AnOlderPartialFrameChangesNothing()
    {
        Guid runId = Guid.NewGuid();
        FleetFrame current = Frame(runId, 5, [1, 2]);

        FleetFrame result = FleetFrameOrdering.Newest(current, Frame(runId, 4, [1]) with { IsPartial = true });

        Assert.AreSame(current, result);
    }

    [TestMethod]
    public async Task TheSourceMergesLiveViewportFramesAndCountsWhatArrived()
    {
        Guid runId = Guid.NewGuid();
        var connection = new SnapshotConnection(Frame(runId, 1, [1, 2, 3]));
        await using var source = new SignalRFleetFrameSource(connection, TimeProvider.System);
        _ = await source.GetFrameAsync(Start);

        await connection.Receive(Frame(runId, 2, [3]) with { IsPartial = true });
        FleetFrame frame = await source.GetFrameAsync(Start);

        Assert.AreEqual(1, source.LastReceivedVehicleCount);
        Assert.AreEqual(3, frame.Vehicles.Count);
        Assert.AreEqual(2, frame.FrameSequence);
    }

    [TestMethod]
    public async Task ViewportCommandsUseTheVoidHubMethods()
    {
        var connection = new SnapshotConnection(Frame(Guid.NewGuid(), 1, [1]));
        await using var source = new SignalRFleetFrameSource(connection, TimeProvider.System);
        var extent = new ViewportExtent(-75.4, 41.0, -75.3, 41.1);

        await source.SetViewportAsync(extent);
        await source.SetSelectedVehicleAsync(null);

        CollectionAssert.AreEqual(new object?[] { "SetViewport", extent, "SetSelectedVehicle", null },
            connection.Sent.SelectMany(sent => new[] { sent.Method, sent.Argument }).ToArray());
    }

    [TestMethod]
    public void AWebMercatorExtentConvertsToDegrees()
    {
        // -75.345, 41.082 projected to Web Mercator meters.
        ViewportExtent extent = ViewportExtents.ToGeographic(-8387367.03, 5024444.18, -8387367.03, 5024444.18, 102100);

        Assert.AreEqual(-75.345, extent.XMin, 0.0001);
        Assert.AreEqual(41.082, extent.YMin, 0.0001);
    }

    [TestMethod]
    public void AGeographicExtentPassesThrough()
    {
        ViewportExtent extent = ViewportExtents.ToGeographic(-75.4, 41.0, -75.3, 41.1, 4326);

        Assert.AreEqual(new ViewportExtent(-75.4, 41.0, -75.3, 41.1), extent);
    }

    private static FleetFrame Frame(Guid runId, long sequence, int[] vehicleIds) =>
        new(runId, sequence, Start, vehicleIds.Select(id =>
            new VehicleReport(id, sequence, Start, -75.34, 41.09, 42, "swiftwater")).ToArray());

    private sealed class SnapshotConnection(FleetFrame snapshot) : IFleetHubConnection
    {
        private Func<FleetFrame, Task>? _receive;

        public HubConnectionState State { get; private set; }

        public List<(string Method, object? Argument)> Sent { get; } = [];

        public Task Receive(FleetFrame frame) => _receive!(frame);

        public IDisposable On<T>(string methodName, Func<T, Task> handler)
        {
            _receive = frame => handler((T)(object)frame);
            return new NoopDisposable();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(string methodName, CancellationToken cancellationToken = default) =>
            Task.FromResult((T)(object)snapshot);

        public Task InvokeAsync(string methodName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendAsync(string methodName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendAsync(string methodName, object? arg1, CancellationToken cancellationToken = default)
        {
            Sent.Add((methodName, arg1));
            return Task.CompletedTask;
        }

        public event Func<Exception?, Task>? Reconnecting { add { } remove { } }

        public event Func<string?, Task>? Reconnected { add { } remove { } }

        public event Func<Exception?, Task>? Closed { add { } remove { } }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
