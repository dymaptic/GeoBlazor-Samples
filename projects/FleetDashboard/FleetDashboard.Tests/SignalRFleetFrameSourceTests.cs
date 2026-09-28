using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class SignalRFleetFrameSourceTests
{
    [TestMethod]
    public async Task ADroppedConnectionHoldsTheVisibleReconnectDelayBeforeRecovering()
    {
        var clock = new FakeTimeProvider();
        Guid runId = Guid.NewGuid();
        var connection = new FakeHubConnection { Snapshot = Frame(runId, 3) };
        await using var source = new SignalRFleetFrameSource(connection, clock);

        FleetFrame connected = await source.GetFrameAsync(clock.GetUtcNow());
        Assert.AreEqual(3, connected.FrameSequence);
        Assert.AreEqual(1, connection.StartAttempts);

        await connection.Drop();
        await connection.RaiseReconnecting();
        // Inside the visible beat the source serves the last known frame and leaves the transport
        // alone, so the audience sees "Reconnecting" instead of a stalled dashboard.
        FleetFrame held = await source.GetFrameAsync(clock.GetUtcNow());
        Assert.AreEqual(3, held.FrameSequence);
        Assert.IsTrue(source.IsReconnecting);
        Assert.AreEqual(1, connection.StartAttempts);

        clock.AdvanceSeconds(1);
        FleetFrame recovered = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreEqual(3, recovered.FrameSequence);
        Assert.AreEqual(2, connection.StartAttempts);
        Assert.IsFalse(source.IsReconnecting);
        Assert.AreEqual(1, source.ReconnectCount);
    }

    [TestMethod]
    public async Task ASnapshotReconcilesAgainstANewerLiveFrameFromTheSameRun()
    {
        var clock = new FakeTimeProvider();
        Guid runId = Guid.NewGuid();
        var connection = new FakeHubConnection { Snapshot = Frame(runId, 4) };
        await using var source = new SignalRFleetFrameSource(connection, clock);

        await connection.RaiseLiveFrame(Frame(runId, 9));
        FleetFrame frame = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreEqual(1, connection.SnapshotRequests);
        Assert.AreEqual(9, frame.FrameSequence);
    }

    [TestMethod]
    public async Task AnAutomaticReconnectMarksTheFrameForSnapshotRefresh()
    {
        var clock = new FakeTimeProvider();
        Guid runId = Guid.NewGuid();
        var connection = new FakeHubConnection { Snapshot = Frame(runId, 12) };
        await using var source = new SignalRFleetFrameSource(connection, clock);
        _ = await source.GetFrameAsync(clock.GetUtcNow());

        await connection.RaiseReconnecting();
        connection.State = HubConnectionState.Connected;
        await connection.RaiseReconnected();
        FleetFrame refreshed = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.IsFalse(source.IsReconnecting);
        Assert.AreEqual(1, source.ReconnectCount);
        Assert.AreEqual(2, connection.SnapshotRequests);
        Assert.AreEqual(12, refreshed.FrameSequence);
    }

    [TestMethod]
    public async Task ARepeatedConnectFailureWaitsForTheBackoffUntilAManualRetry()
    {
        var clock = new FakeTimeProvider();
        var connection = new FakeHubConnection
        {
            Snapshot = Frame(Guid.NewGuid(), 1),
            StartFailure = new InvalidOperationException("The fleet service is unavailable.")
        };
        await using var source = new SignalRFleetFrameSource(connection, clock);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => source.GetFrameAsync(clock.GetUtcNow()).AsTask());
        Assert.AreEqual(1, connection.StartAttempts);

        // Inside the retry interval the source refuses before touching the transport again.
        InvalidOperationException backoff = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => source.GetFrameAsync(clock.GetUtcNow()).AsTask());
        StringAssert.Contains(backoff.Message, "Still trying");
        Assert.AreEqual(1, connection.StartAttempts);

        connection.StartFailure = null;
        await source.RetryConnectionAsync();
        FleetFrame frame = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreEqual(2, connection.StartAttempts);
        Assert.AreEqual(1, frame.FrameSequence);
    }

    private static FleetFrame Frame(Guid runId, long frameSequence)
    {
        DateTimeOffset publishedAtUtc = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        return new FleetFrame(runId, frameSequence, publishedAtUtc,
            [new VehicleReport(7, frameSequence, publishedAtUtc, -75.34, 41.09, 42, "swiftwater")]);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void AdvanceSeconds(double seconds) => _utcNow = _utcNow.AddSeconds(seconds);
    }

    private sealed class FakeHubConnection : IFleetHubConnection
    {
        public HubConnectionState State { get; set; } = HubConnectionState.Disconnected;

        public int StartAttempts { get; private set; }

        public int SnapshotRequests { get; private set; }

        public Exception? StartFailure { get; set; }

        public FleetFrame? Snapshot { get; set; }

        public event Func<Exception?, Task>? Reconnecting;
        public event Func<string?, Task>? Reconnected;
        public event Func<Exception?, Task>? Closed;

        public IDisposable On<T>(string methodName, Func<T, Task> handler)
        {
            if (methodName == "ReceiveFleetFrame" && typeof(T) == typeof(FleetFrame))
            {
                _receiveFrame = frame => handler((T)(object)frame);
            }

            return new NoopDisposable();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartAttempts++;
            if (StartFailure is not null) throw StartFailure;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(string methodName, CancellationToken cancellationToken = default)
        {
            SnapshotRequests++;
            return Task.FromResult((T)(object)Snapshot!);
        }

        public Task SendAsync(string methodName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendAsync(string methodName, object? arg1, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task Drop()
        {
            State = HubConnectionState.Disconnected;
            return Closed?.Invoke(null) ?? Task.CompletedTask;
        }

        public Task RaiseReconnecting() => Reconnecting?.Invoke(null) ?? Task.CompletedTask;

        public Task RaiseReconnected() => Reconnected?.Invoke(null) ?? Task.CompletedTask;

        public Task RaiseLiveFrame(FleetFrame frame) =>
            _receiveFrame is null ? Task.CompletedTask : _receiveFrame(frame);

        private Func<FleetFrame, Task>? _receiveFrame;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
