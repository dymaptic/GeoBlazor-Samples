using FleetDashboard.Client.State;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetDashboardStateTests
{
    [TestMethod]
    public void CaptureDirtyReturnsTheNewestPendingReportPerVehicle()
    {
        var state = new FleetDashboardState();
        state.Accept(new FleetFrame(RunId, 1, Instant, [Report(7, 0)]));
        state.Accept(new FleetFrame(RunId, 2, Instant.AddSeconds(1), [Report(7, 1)]));

        PendingFleetBatch batch = state.CaptureDirty();

        Assert.HasCount(1, batch.Reports);
        Assert.AreEqual(2, batch.Reports[0].Sequence);
        Assert.AreEqual(1L, state.SupersededPendingReportCount);
    }

    [TestMethod]
    public void HistoryExpiresWhileReportsArePaused()
    {
        var state = new FleetDashboardState();
        state.Accept(new FleetFrame(RunId, 1, Instant, [Report(7)]));
        state.Select(7);

        Assert.HasCount(0, state.CreateView(Instant.AddSeconds(61)).SelectedHistory);
    }

    [TestMethod]
    public void TimerJitterDoesNotDropAReportingSecond()
    {
        var state = new FleetDashboardState();
        double[] seconds = [0, 1.01, 2, 3.01];
        for (int index = 0; index < seconds.Length; index++)
        {
            var report = Report(7, index) with { ObservedAtUtc = Instant.AddSeconds(seconds[index]) };
            state.Accept(new FleetFrame(RunId, index + 1, report.ObservedAtUtc, [report]));
        }
        state.Select(7);

        Assert.HasCount(4, state.CreateView(Instant.AddSeconds(4)).SelectedHistory);
    }

    [TestMethod]
    public void VehicleBecomesStaleAfterFiveSecondsWithoutANewObservation()
    {
        var state = new FleetDashboardState();
        state.Accept(new FleetFrame(RunId, 1, Instant, [Report(7)]));
        state.Select(7);

        Assert.AreEqual(VehicleStatus.Moving, state.CreateView(Instant.AddSeconds(5)).SelectedVehicle!.Status);
        Assert.AreEqual(VehicleStatus.Stale,
            state.CreateView(Instant.AddSeconds(5).AddTicks(1)).SelectedVehicle!.Status);
    }

    [TestMethod]
    public void StatusChangeWithoutANewReportQueuesTheVehicleForTheMap()
    {
        var state = new FleetDashboardState();
        state.Accept(new FleetFrame(RunId, 1, Instant, [Report(7)]));
        state.CreateView(Instant);
        state.CaptureDirty();

        state.CreateView(Instant.AddSeconds(1));
        Assert.HasCount(0, state.CaptureDirty().Reports);

        state.CreateView(Instant.AddSeconds(6));
        PendingFleetBatch batch = state.CaptureDirty();
        Assert.HasCount(1, batch.Reports);
        Assert.AreEqual(7, batch.Reports[0].VehicleId);
    }

    [TestMethod]
    public void HistoryKeepsOnlySixtyOneSecondSamples()
    {
        var state = new FleetDashboardState();
        for (int second = 0; second < 75; second++)
        {
            state.Accept(new FleetFrame(RunId, second + 1, Instant.AddSeconds(second), [Report(7, second)]));
        }

        state.Select(7);
        FleetDashboardView view = state.CreateView(Instant.AddSeconds(74));
        Assert.HasCount(60, view.SelectedHistory);
        Assert.AreEqual(Instant.AddSeconds(15), view.SelectedHistory[0].ObservedAtUtc);
        Assert.AreEqual(Instant.AddSeconds(74), view.SelectedHistory[^1].ObservedAtUtc);
    }

    [TestMethod]
    public void SelectionResolvesTheSameVehicleView()
    {
        var state = new FleetDashboardState();
        state.Accept(new FleetFrame(RunId, 1, Instant, [Report(7), Report(2)]));
        state.Select(7);
        FleetDashboardView view = state.CreateView(Instant);

        Assert.AreSame(view.Vehicles.Single(v => v.VehicleId == 7), view.SelectedVehicle);
        state.Select(null);
        Assert.IsNull(state.CreateView(Instant).SelectedVehicle);
    }

    [TestMethod]
    public void AcceptingAFrameExposesAStableSortedRoster()
    {
        var state = new FleetDashboardState();
        state.Accept(new FleetFrame(RunId, 2, Instant, [Report(7), Report(2)]));
        state.Accept(new FleetFrame(RunId, 1, Instant, [Report(99)]));
        FleetDashboardView view = state.CreateView(Instant);

        CollectionAssert.AreEqual(new[] { 2, 7 }, view.Vehicles.Select(v => v.VehicleId).ToArray());
        Assert.IsTrue(view.HasCurrentSnapshot);
        Assert.AreEqual(Instant, view.LastFrameAtUtc);
    }

    [TestMethod]
    public void HistorySamplesAtMostOncePerSecondPerVehicle()
    {
        var state = new FleetDashboardState();
        // Reports every half second: the chart window stays one sample per second.
        for (int halfSecond = 0; halfSecond < 10; halfSecond++)
        {
            VehicleReport report = Report(7, halfSecond) with
            {
                ObservedAtUtc = Instant.AddSeconds(halfSecond * 0.5)
            };
            state.Accept(new FleetFrame(RunId, halfSecond + 1, report.ObservedAtUtc, [report]));
        }
        state.Select(7);

        FleetDashboardView view = state.CreateView(Instant.AddSeconds(4.5));
        CollectionAssert.AreEqual(new[] { 0d, 1, 2, 3, 4 },
            view.SelectedHistory.Select(s => (s.ObservedAtUtc - Instant).TotalSeconds).ToArray());
    }

    private static VehicleReport Report(int id, int second = 0)
    {
        return new VehicleReport(id, second + 1, Instant.AddSeconds(second), -75.34, 41.09, 42, "swiftwater");
    }

    private static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
    private static readonly Guid RunId = Guid.Parse("10000000-0000-0000-0000-000000000001");
}
