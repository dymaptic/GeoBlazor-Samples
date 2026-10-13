using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;
using FleetDashboard.Shared.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetFrameBuilderTests
{
    [TestMethod]
    public void BeforeTheFirstFrameTheSnapshotIsEmpty()
    {
        var store = new VehicleReportStore();
        store.Record(Report(1, 1));
        var builder = new FleetFrameBuilder(store, new FakeTimeProvider());

        Assert.AreEqual(0, builder.CurrentFrame.FrameSequence);
        Assert.IsEmpty(builder.CurrentFrame.Vehicles);
    }

    [TestMethod]
    public void AFrameIsStampedWithTheRunSequenceAndTimeAndCarriesEveryStoredReport()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        store.Record(Report(2, 4));
        store.Record(Report(1, 9));
        var builder = new FleetFrameBuilder(store, clock);

        FleetFrame frame = builder.CreateFrame();

        Assert.AreEqual(builder.CurrentFrame.RunId, frame.RunId);
        Assert.AreNotEqual(Guid.Empty, frame.RunId);
        Assert.AreEqual(1, frame.FrameSequence);
        Assert.AreEqual(clock.GetUtcNow(), frame.PublishedAtUtc);
        CollectionAssert.AreEqual(store.Latest().ToArray(), frame.Vehicles.ToArray());
        Assert.AreSame(frame, builder.CurrentFrame);
    }

    [TestMethod]
    public void TheFrameSequenceIncrementsWithinARun()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        store.Record(Report(1, 1));
        var builder = new FleetFrameBuilder(store, clock);

        FleetFrame first = builder.CreateFrame();
        clock.AdvanceSeconds(1);
        FleetFrame second = builder.CreateFrame();

        Assert.AreEqual(first.RunId, second.RunId);
        Assert.AreEqual(first.FrameSequence + 1, second.FrameSequence);
        Assert.IsTrue(second.PublishedAtUtc > first.PublishedAtUtc);
    }

    [TestMethod]
    public void StartingANewRunChangesTheRunIdAndRestartsAtFrameOne()
    {
        var store = new VehicleReportStore();
        store.Record(Report(1, 1));
        var builder = new FleetFrameBuilder(store, new FakeTimeProvider());
        _ = builder.CreateFrame();
        FleetFrame lastOfOldRun = builder.CreateFrame();

        builder.StartNewRun();

        // The snapshot stays on the old run until the next frame is built.
        Assert.AreSame(lastOfOldRun, builder.CurrentFrame);

        FleetFrame firstOfNewRun = builder.CreateFrame();
        Assert.AreNotEqual(lastOfOldRun.RunId, firstOfNewRun.RunId);
        Assert.AreEqual(1, firstOfNewRun.FrameSequence);
    }

    [TestMethod]
    public void APausedTrucksReportRepeatsUnchangedAcrossFrames()
    {
        var clock = new FakeTimeProvider();
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        var builder = new FleetFrameBuilder(store, clock);
        simulator.PauseVehicle(7);
        FleetFrame first = builder.CreateFrame();

        clock.AdvanceSeconds(5);
        simulator.Advance();
        FleetFrame second = builder.CreateFrame();

        Assert.HasCount(ScenarioSettings.Normal.VehicleCount, second.Vehicles);
        Assert.AreEqual(first.Vehicles.Single(v => v.VehicleId == 7), second.Vehicles.Single(v => v.VehicleId == 7));
        Assert.AreEqual(first.Vehicles.Single(v => v.VehicleId == 8).Sequence + 1,
            second.Vehicles.Single(v => v.VehicleId == 8).Sequence);
    }

    private static VehicleReport Report(int vehicleId, long sequence) =>
        new(vehicleId, sequence, Instant, -75.34, 41.09, 42, "swiftwater");

    private static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2026-09-09T12:00:00Z");

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void AdvanceSeconds(double seconds) => _utcNow = _utcNow.AddSeconds(seconds);
    }
}
