using FleetDashboard.Server.Simulation;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Server.Telemetry;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetBroadcasterTests
{
    [TestMethod]
    public async Task BroadcastsBuiltFramesOnTheClientMethodContract()
    {
        using var store = new VehicleReportStore();
        // The simulator's constructor fills the store; its reporting loop is not needed here.
        _ = new FleetSimulator(TimeProvider.System, ScenarioSettings.Normal, store);
        var frameBuilder = new FleetFrameBuilder(store, TimeProvider.System);
        var hubContext = new RecordingHubContext();
        var broadcaster = new FleetBroadcaster(frameBuilder, ScenarioSettings.Normal, hubContext,
            new ViewportSubscriptions());

        await broadcaster.StartAsync(CancellationToken.None);
        FleetFrame frame = await hubContext.NextFrame.WaitAsync(TimeSpan.FromSeconds(15));
        await broadcaster.StopAsync(CancellationToken.None);

        Assert.AreEqual("ReceiveFleetFrame", hubContext.MethodName);
        Assert.AreEqual(ScenarioSettings.Normal.VehicleCount, frame.Vehicles.Count);
    }

    [TestMethod]
    public async Task SendsAPopulatedFrameAtStartRatherThanWaitingForTheFirstTick()
    {
        using var store = new VehicleReportStore();
        _ = new FleetSimulator(TimeProvider.System, ScenarioSettings.Normal, store);
        var frameBuilder = new FleetFrameBuilder(store, TimeProvider.System);
        var hubContext = new RecordingHubContext();
        var broadcaster = new FleetBroadcaster(frameBuilder, ScenarioSettings.Normal, hubContext,
            new ViewportSubscriptions());

        await broadcaster.StartAsync(CancellationToken.None);
        // Normal ticks every 500 ms, so a frame within 200 ms was sent before any tick.
        FleetFrame frame = await hubContext.NextFrame.WaitAsync(TimeSpan.FromMilliseconds(200));
        await broadcaster.StopAsync(CancellationToken.None);

        Assert.AreEqual(1, frame.FrameSequence);
        Assert.AreEqual(ScenarioSettings.Normal.VehicleCount, frame.Vehicles.Count);
    }

    [TestMethod]
    public async Task BuildsEachFrameWhenItIsSentSoASlowSendNeverQueuesAStaleOne()
    {
        using var store = new VehicleReportStore();
        _ = new FleetSimulator(TimeProvider.System, ScenarioSettings.Normal, store);
        var frameBuilder = new FleetFrameBuilder(store, TimeProvider.System);
        var hubContext = new RecordingHubContext
        {
            HoldFirstSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var broadcaster = new FleetBroadcaster(frameBuilder, ScenarioSettings.Normal, hubContext,
            new ViewportSubscriptions());

        await broadcaster.StartAsync(CancellationToken.None);
        await hubContext.NextFrame.WaitAsync(TimeSpan.FromSeconds(15));

        // Normal publishes 2 frames a second. Holding the first send across four intervals is
        // where a frame queue would build frames, drop some, and later send a superseded one.
        await Task.Delay(TimeSpan.FromSeconds(2.1));
        DateTimeOffset releasedAt = TimeProvider.System.GetUtcNow();
        hubContext.HoldFirstSend!.SetResult();
        await WaitUntil(() => hubContext.SentFrames.Count >= 3, TimeSpan.FromSeconds(15));
        await broadcaster.StopAsync(CancellationToken.None);

        FleetFrame[] sent = hubContext.SentFrames.Take(3).ToArray();

        // No frame number was skipped, so nothing was built and dropped.
        long[] sequences = sent.Select(frame => frame.FrameSequence).ToArray();
        CollectionAssert.AreEqual(new[] { sequences[0], sequences[0] + 1, sequences[0] + 2 }, sequences);

        // A queue that built frames during the hold would also number them consecutively, so
        // check the clock too: each frame sent after the release was built after the release.
        foreach (FleetFrame frame in sent.Skip(1))
        {
            Assert.IsTrue(frame.PublishedAtUtc >= releasedAt,
                $"Frame {frame.FrameSequence} was built at {frame.PublishedAtUtc:O}, before the hold ended at {releasedAt:O}.");
        }
    }

    private static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) Assert.Fail("Timed out waiting for the broadcaster.");
            await Task.Delay(50);
        }
    }
}
