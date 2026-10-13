using FleetDashboard.Shared;
using FleetDashboard.Shared.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FleetDashboard.Tests;

[TestClass]
public sealed class FleetSimulatorTests
{
    [TestMethod]
    public void TheStoreHoldsEveryVehicleAtSequenceOneBeforeTheFirstAdvance()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        _ = new FleetSimulator(clock, ScenarioSettings.Normal, store);

        IReadOnlyList<VehicleReport> reports = store.Latest();
        Assert.HasCount(ScenarioSettings.Normal.VehicleCount, reports);
        Assert.IsTrue(reports.All(r => r.Sequence == 1 && r.ObservedAtUtc == clock.GetUtcNow()));
    }

    [TestMethod]
    public void PausingOneVehicleLeavesItsStoredReportUnchangedWhileOthersAdvance()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        simulator.Advance();
        VehicleReport heldBefore = Stored(store, 7);
        VehicleReport movingBefore = Stored(store, 8);

        simulator.PauseVehicle(7);
        clock.AdvanceSeconds(10);
        simulator.Advance();

        Assert.AreEqual(heldBefore, Stored(store, 7));

        VehicleReport movingAfter = Stored(store, 8);
        Assert.AreEqual(movingBefore.Sequence + 1, movingAfter.Sequence);
        Assert.IsTrue(movingAfter.ObservedAtUtc > movingBefore.ObservedAtUtc);
    }

    [TestMethod]
    public void ResumedVehicleReportsTheNextSequenceWithoutAPositionJump()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        simulator.Advance();

        simulator.PauseVehicle(7);
        clock.AdvanceSeconds(10);
        simulator.Advance();
        VehicleReport held = Stored(store, 7);

        simulator.ResumeVehicle(7);
        simulator.Advance();

        VehicleReport report = Stored(store, 7);
        Assert.AreEqual(held.Sequence + 1, report.Sequence);
        Assert.AreEqual(held.Longitude, report.Longitude);
        Assert.AreEqual(held.Latitude, report.Latitude);
    }

    [TestMethod]
    public void PausingTheFleetWritesNothingAndFreezesSimulatedTime()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        simulator.Advance();
        IReadOnlyList<VehicleReport> before = store.Latest();

        simulator.PauseFleet();
        clock.AdvanceSeconds(10);
        simulator.Advance();

        CollectionAssert.AreEqual(before.ToArray(), store.Latest().ToArray());

        // Resuming continues from where the fleet stopped; the paused ten seconds are not replayed.
        simulator.ResumeFleet();
        simulator.Advance();

        foreach (VehicleReport paused in before)
        {
            VehicleReport resumed = Stored(store, paused.VehicleId);
            Assert.AreEqual(paused.Sequence + 1, resumed.Sequence);
            Assert.AreEqual(paused.Longitude, resumed.Longitude);
            Assert.AreEqual(paused.Latitude, resumed.Latitude);
        }
    }

    [TestMethod]
    public void ResetRestartsEveryVehicleAtSequenceOneAndItsStartingPosition()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        IReadOnlyList<VehicleReport> initial = store.Latest();
        simulator.Advance();
        clock.AdvanceSeconds(10);
        simulator.Advance();
        simulator.PauseVehicle(7);
        simulator.PauseFleet();

        simulator.Reset();

        IReadOnlyList<VehicleReport> after = store.Latest();
        Assert.HasCount(ScenarioSettings.Normal.VehicleCount, after);
        for (int index = 0; index < after.Count; index++)
        {
            Assert.AreEqual(1, after[index].Sequence);
            Assert.AreEqual(clock.GetUtcNow(), after[index].ObservedAtUtc);
            Assert.AreEqual(initial[index].Longitude, after[index].Longitude);
            Assert.AreEqual(initial[index].Latitude, after[index].Latitude);
        }
    }

    [TestMethod]
    public void ConfiguringTheStressPresetGrowsTheRosterOnTheNextReport()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        simulator.Advance();

        simulator.Configure(ScenarioSettings.Stress);
        simulator.Advance();

        Assert.HasCount(500, store.Latest());
        Assert.AreEqual(1, Stored(store, 500).Sequence);
        Assert.IsTrue(Stored(store, 7).Sequence > 1);
    }

    [TestMethod]
    public void ConfiguringTheNormalPresetShrinksTheStore()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Stress, store);
        simulator.Advance();

        simulator.Configure(ScenarioSettings.Normal);

        Assert.HasCount(50, store.Latest());
        Assert.IsFalse(store.Latest().Any(r => r.VehicleId > 50));

        simulator.Advance();
        Assert.HasCount(50, store.Latest());
    }

    [TestMethod]
    public void TheConfiguredPresetExposesTheReportAndFrameRates()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        Assert.AreEqual(2, simulator.Settings.ReportsPerSecond);
        Assert.AreEqual(2, simulator.Settings.FramesPerSecond);

        simulator.Configure(ScenarioSettings.Stress);
        Assert.AreEqual(10, simulator.Settings.ReportsPerSecond);
        Assert.AreEqual(10, simulator.Settings.FramesPerSecond);
        Assert.AreEqual(500, simulator.Settings.VehicleCount);
    }

    private static VehicleReport Stored(VehicleReportStore store, int vehicleId) =>
        store.Latest().Single(r => r.VehicleId == vehicleId);

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void AdvanceSeconds(double seconds) => _utcNow = _utcNow.AddSeconds(seconds);
    }
}
