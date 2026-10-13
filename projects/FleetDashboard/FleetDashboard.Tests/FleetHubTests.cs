using FleetDashboard.Server;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;
using FleetDashboard.Shared.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetHubTests
{
    [TestMethod]
    public void ResetNeverLetsANewRunFrameCarryTheOldRunsSequences()
    {
        var simulatorClock = new InterleavingTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(simulatorClock, ScenarioSettings.Normal, store);
        var frameBuilder = new FleetFrameBuilder(store, TimeProvider.System);
        var viewports = new ViewportSubscriptions();
        var broadcaster = new FleetBroadcaster(frameBuilder, ScenarioSettings.Normal, new RecordingHubContext(),
            viewports);
        var hub = new FleetHub(simulator, frameBuilder, broadcaster, viewports);
        simulator.Advance();
        simulator.Advance();
        FleetFrame oldRun = frameBuilder.CreateFrame();

        // Build a frame in the middle of the reset, the way the broadcaster loop could, to prove
        // that a frame racing the reset is still labeled with the old run.
        FleetFrame? duringReset = null;
        simulatorClock.OnNextRead = () => duringReset = frameBuilder.CreateFrame();

        hub.Reset();
        FleetFrame newRun = frameBuilder.CreateFrame();

        Assert.IsNotNull(duringReset, "The simulator no longer reads the clock during Reset; move the hook.");
        Assert.AreEqual(oldRun.RunId, duringReset.RunId);
        Assert.AreNotEqual(oldRun.RunId, newRun.RunId);
        Assert.AreEqual(1, newRun.FrameSequence);
        Assert.HasCount(ScenarioSettings.Normal.VehicleCount, newRun.Vehicles);
        Assert.IsTrue(newRun.Vehicles.All(v => v.Sequence == 1));
    }

    /// <summary>The system clock, plus a one-shot callback on the next read, so a test can run code
    /// inside the simulator's Reset at the moment it reads the clock.</summary>
    private sealed class InterleavingTimeProvider : TimeProvider
    {
        public Action? OnNextRead { get; set; }

        public override DateTimeOffset GetUtcNow()
        {
            Action? callback = OnNextRead;
            OnNextRead = null;
            callback?.Invoke();
            return TimeProvider.System.GetUtcNow();
        }
    }
}
