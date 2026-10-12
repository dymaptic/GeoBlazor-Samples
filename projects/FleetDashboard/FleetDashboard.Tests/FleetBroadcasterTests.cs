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

        // Normal publishes 2 frames a second. Holding the first send across three intervals is
        // where a frame queue would build frames, drop some, and later send a superseded one.
        await Task.Delay(TimeSpan.FromSeconds(1.6));
        hubContext.HoldFirstSend!.SetResult();
        await WaitUntil(() => hubContext.SentFrameSequences.Count >= 3, TimeSpan.FromSeconds(15));
        await broadcaster.StopAsync(CancellationToken.None);

        // Frames are built only when they are sent, so no frame number was skipped.
        long[] sent = hubContext.SentFrameSequences.Take(3).ToArray();
        CollectionAssert.AreEqual(new[] { sent[0], sent[0] + 1, sent[0] + 2 }, sent);
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
