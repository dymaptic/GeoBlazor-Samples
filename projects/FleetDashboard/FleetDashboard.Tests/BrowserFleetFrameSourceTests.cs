using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class BrowserFleetFrameSourceTests
{
    [TestMethod]
    public async Task TheFirstCallBuildsAPopulatedFrameAtOnce()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);

        FleetFrame frame = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreEqual(1, frame.FrameSequence);
        Assert.HasCount(ScenarioSettings.Normal.VehicleCount, frame.Vehicles);
        Assert.IsFalse(frame.IsPartial);
    }

    [TestMethod]
    public async Task ACallBeforeTheNextTickServesTheSameFrame()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        FleetFrame first = await source.GetFrameAsync(clock.GetUtcNow());

        clock.AdvanceSeconds(0.25);
        FleetFrame repeat = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreSame(first, repeat);
    }

    [TestMethod]
    public async Task EveryVehicleSequenceAdvancesIndependentlyOfTheFrameSequence()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        FleetFrame first = await source.GetFrameAsync(clock.GetUtcNow());

        // Vehicle 7 pauses, so its sequence must hold while the frame counter and every other vehicle move.
        await source.PauseVehicleAsync(7);
        FleetFrame previous = first;
        for (int tick = 0; tick < 3; tick++)
        {
            clock.AdvanceSeconds(0.5);
            FleetFrame next = await source.GetFrameAsync(clock.GetUtcNow());

            Assert.AreEqual(previous.FrameSequence + 1, next.FrameSequence);
            Assert.AreEqual(previous.RunId, next.RunId);
            foreach (VehicleReport report in next.Vehicles)
            {
                long before = previous.Vehicles.Single(v => v.VehicleId == report.VehicleId).Sequence;
                Assert.AreEqual(report.VehicleId == 7 ? before : before + 1, report.Sequence);
            }

            previous = next;
        }
    }

    [TestMethod]
    public async Task ResumingAVehicleContinuesItsSequenceWithoutAGap()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        await source.GetFrameAsync(clock.GetUtcNow());
        await source.PauseVehicleAsync(7);
        clock.AdvanceSeconds(0.5);
        FleetFrame held = await source.GetFrameAsync(clock.GetUtcNow());

        await source.ResumeVehicleAsync(7);
        clock.AdvanceSeconds(0.5);
        FleetFrame resumed = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreEqual(held.Vehicles.Single(v => v.VehicleId == 7).Sequence + 1,
            resumed.Vehicles.Single(v => v.VehicleId == 7).Sequence);
    }

    [TestMethod]
    public async Task PausingTheFleetKeepsFramesComingWithoutNewReports()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        FleetFrame before = await source.GetFrameAsync(clock.GetUtcNow());

        await source.PauseFleetAsync();
        clock.AdvanceSeconds(0.5);
        FleetFrame paused = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreEqual(before.FrameSequence + 1, paused.FrameSequence);
        CollectionAssert.AreEqual(before.Vehicles.ToArray(), paused.Vehicles.ToArray());

        await source.ResumeFleetAsync();
        clock.AdvanceSeconds(0.5);
        FleetFrame resumed = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.IsTrue(resumed.Vehicles.All(v =>
            v.Sequence == before.Vehicles.Single(b => b.VehicleId == v.VehicleId).Sequence + 1));
    }

    [TestMethod]
    public async Task ResetStartsANewRunWithEveryVehicleAtSequenceOne()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        await source.GetFrameAsync(clock.GetUtcNow());
        clock.AdvanceSeconds(0.5);
        FleetFrame oldRun = await source.GetFrameAsync(clock.GetUtcNow());
        await source.PauseFleetAsync();

        await source.ResetAsync();
        FleetFrame newRun = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.AreNotEqual(oldRun.RunId, newRun.RunId);
        Assert.AreEqual(1, newRun.FrameSequence);
        Assert.HasCount(ScenarioSettings.Normal.VehicleCount, newRun.Vehicles);
        Assert.IsTrue(newRun.Vehicles.All(v => v.Sequence == 1));

        // Reset also clears the fleet pause, so the next tick moves every vehicle on.
        clock.AdvanceSeconds(0.5);
        FleetFrame next = await source.GetFrameAsync(clock.GetUtcNow());
        Assert.IsTrue(next.Vehicles.All(v => v.Sequence == 2));
    }

    [TestMethod]
    public async Task ApplyingTheStressPresetGrowsTheRosterOnTheNextTick()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        await source.GetFrameAsync(clock.GetUtcNow());

        await source.SetPresetAsync("stress");
        clock.AdvanceSeconds(0.5);
        FleetFrame frame = await source.GetFrameAsync(clock.GetUtcNow());

        Assert.HasCount(ScenarioSettings.Stress.VehicleCount, frame.Vehicles);
    }

    [TestMethod]
    public async Task ReportsKeepThePresetRateWhenThePageOnlyPollsEveryQuarterSecond()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        await source.SetPresetAsync("stress");
        FleetFrame start = await source.GetFrameAsync(clock.GetUtcNow());

        FleetFrame latest = start;
        for (int poll = 0; poll < 8; poll++)
        {
            clock.AdvanceSeconds(0.25);
            latest = await source.GetFrameAsync(clock.GetUtcNow());
        }

        // Stress reports 10 times a second, so two seconds of polling yields 20 reports per vehicle.
        VehicleReport first = start.Vehicles.Single(v => v.VehicleId == 1);
        VehicleReport last = latest.Vehicles.Single(v => v.VehicleId == 1);
        Assert.AreEqual(first.Sequence + 20, last.Sequence);
        Assert.AreEqual(clock.GetUtcNow(), last.ObservedAtUtc);
    }

    [TestMethod]
    public async Task AfterALongGapOnePollRunsAtMostOneSecondOfReportsAndThenResumesNormally()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        await source.SetPresetAsync("stress");
        FleetFrame start = await source.GetFrameAsync(clock.GetUtcNow());

        clock.AdvanceSeconds(5 * 60);
        FleetFrame afterGap = await source.GetFrameAsync(clock.GetUtcNow());

        VehicleReport first = start.Vehicles.Single(v => v.VehicleId == 1);
        VehicleReport caughtUp = afterGap.Vehicles.Single(v => v.VehicleId == 1);
        Assert.AreEqual(first.Sequence + ScenarioSettings.Stress.ReportsPerSecond, caughtUp.Sequence);
        Assert.AreEqual(clock.GetUtcNow(), caughtUp.ObservedAtUtc);

        clock.AdvanceSeconds(0.1);
        FleetFrame next = await source.GetFrameAsync(clock.GetUtcNow());
        Assert.AreEqual(caughtUp.Sequence + 1, next.Vehicles.Single(v => v.VehicleId == 1).Sequence);
    }

    [TestMethod]
    public async Task AnUnknownPresetIsRejected()
    {
        var source = new BrowserFleetFrameSource(new FakeTimeProvider());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => source.SetPresetAsync("turbo"));
    }

    [TestMethod]
    public async Task BreakingTheConnectionDoesNothing()
    {
        var clock = new FakeTimeProvider();
        var source = new BrowserFleetFrameSource(clock);
        FleetFrame before = await source.GetFrameAsync(clock.GetUtcNow());

        await source.BreakConnectionAsync();

        Assert.AreSame(before, await source.GetFrameAsync(clock.GetUtcNow()));
    }

    [TestMethod]
    public void TheSourceOffersNoConnectionOrViewportFeatures()
    {
        var source = new BrowserFleetFrameSource(new FakeTimeProvider());

        Assert.IsNotInstanceOfType<IFleetConnectionStatus>(source);
        Assert.IsNotInstanceOfType<IFleetViewportControls>(source);
        Assert.IsInstanceOfType<IFleetScenarioControls>(source);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void AdvanceSeconds(double seconds) => _utcNow = _utcNow.AddSeconds(seconds);
    }
}
