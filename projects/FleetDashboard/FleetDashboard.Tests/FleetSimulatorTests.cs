using FleetDashboard.Server.Simulation;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FleetDashboard.Tests;

[TestClass]
public sealed class FleetSimulatorTests
{
    [TestMethod]
    public void PausingOneVehicleDoesNotPauseTheOthers()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal);
        FleetFrame first = simulator.Advance();

        simulator.PauseVehicle(7);
        clock.AdvanceSeconds(10);
        FleetFrame second = simulator.Advance();

        VehicleReport heldBefore = first.Vehicles.Single(v => v.VehicleId == 7);
        VehicleReport heldAfter = second.Vehicles.Single(v => v.VehicleId == 7);
        Assert.AreEqual(heldBefore.Sequence, heldAfter.Sequence);
        Assert.AreEqual(heldBefore.ObservedAtUtc, heldAfter.ObservedAtUtc);
        Assert.AreEqual(heldBefore.Longitude, heldAfter.Longitude);
        Assert.AreEqual(heldBefore.Latitude, heldAfter.Latitude);

        VehicleReport movingBefore = first.Vehicles.Single(v => v.VehicleId == 8);
        VehicleReport movingAfter = second.Vehicles.Single(v => v.VehicleId == 8);
        Assert.AreEqual(movingBefore.Sequence + 1, movingAfter.Sequence);
        Assert.IsTrue(movingAfter.ObservedAtUtc > movingBefore.ObservedAtUtc);
    }

    [TestMethod]
    public void ResumedVehicleProducesANewObservationWithoutAPositionJump()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal);
        _ = simulator.Advance();

        simulator.PauseVehicle(7);
        clock.AdvanceSeconds(10);
        FleetFrame paused = simulator.Advance();

        simulator.ResumeVehicle(7);
        FleetFrame resumed = simulator.Advance();

        VehicleReport held = paused.Vehicles.Single(v => v.VehicleId == 7);
        VehicleReport report = resumed.Vehicles.Single(v => v.VehicleId == 7);
        Assert.AreEqual(held.Sequence + 1, report.Sequence);
        Assert.AreEqual(held.Longitude, report.Longitude);
        Assert.AreEqual(held.Latitude, report.Latitude);
    }

    [TestMethod]
    public void PausingTheFleetFreezesObservationsWhileFramesContinue()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal);
        FleetFrame first = simulator.Advance();

        simulator.PauseFleet();
        clock.AdvanceSeconds(10);
        FleetFrame frozen = simulator.Advance();

        Assert.AreEqual(first.FrameSequence + 1, frozen.FrameSequence);
        foreach (VehicleReport before in first.Vehicles)
        {
            VehicleReport after = frozen.Vehicles.Single(v => v.VehicleId == before.VehicleId);
            Assert.AreEqual(before.Sequence, after.Sequence);
            Assert.AreEqual(before.ObservedAtUtc, after.ObservedAtUtc);
            Assert.AreEqual(before.Longitude, after.Longitude);
        }
    }

    [TestMethod]
    public void ResetCreatesANewRunAndRestartsSequenceCounters()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal);
        _ = simulator.Advance();
        clock.AdvanceSeconds(10);
        _ = simulator.Advance();
        simulator.PauseVehicle(7);
        Guid previousRunId = simulator.CurrentFrame.RunId;

        simulator.Reset();
        FleetFrame after = simulator.CurrentFrame;

        Assert.AreNotEqual(previousRunId, after.RunId);
        Assert.AreEqual(1, after.FrameSequence);
        Assert.AreEqual(1, after.Vehicles.Single(v => v.VehicleId == 7).Sequence);
    }

    [TestMethod]
    public void ConfiguringTheStressPresetGrowsTheRosterOnTheNextFrame()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal);
        _ = simulator.Advance();

        simulator.Configure(ScenarioSettings.Stress);
        FleetFrame frame = simulator.Advance();

        Assert.HasCount(500, frame.Vehicles);
        Assert.AreEqual(1, frame.Vehicles.Single(v => v.VehicleId == 500).Sequence);
        Assert.IsTrue(frame.Vehicles.Single(v => v.VehicleId == 7).Sequence > 1);
    }

    [TestMethod]
    public void ConfiguringTheNormalPresetShrinksTheRoster()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Stress);
        _ = simulator.Advance();

        simulator.Configure(ScenarioSettings.Normal);
        FleetFrame frame = simulator.Advance();

        Assert.HasCount(50, frame.Vehicles);
        Assert.IsFalse(frame.Vehicles.Any(v => v.VehicleId > 50));
    }

    [TestMethod]
    public void TheConfiguredPresetIsExposedForTheBroadcasterRate()
    {
        var clock = new FakeTimeProvider();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal);
        Assert.AreEqual(2, simulator.Settings.FramesPerSecond);

        simulator.Configure(ScenarioSettings.Stress);
        Assert.AreEqual(10, simulator.Settings.FramesPerSecond);
        Assert.AreEqual(500, simulator.Settings.VehicleCount);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void AdvanceSeconds(double seconds) => _utcNow = _utcNow.AddSeconds(seconds);
    }
}
