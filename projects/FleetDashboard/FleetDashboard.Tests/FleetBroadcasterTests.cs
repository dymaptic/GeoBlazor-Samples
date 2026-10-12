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
}
