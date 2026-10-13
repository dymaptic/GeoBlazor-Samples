using FleetDashboard.Shared;
using FleetDashboard.Shared.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class VehicleReportStoreTests
{
    [TestMethod]
    public void RecordingAVehicleAgainKeepsOnlyItsLatestReport()
    {
        var store = new VehicleReportStore();

        store.Record(Report(7, 1));
        store.Record(Report(7, 2));

        VehicleReport stored = store.Latest().Single();
        Assert.AreEqual(7, stored.VehicleId);
        Assert.AreEqual(2, stored.Sequence);
    }

    [TestMethod]
    public void LatestIsOrderedByVehicleId()
    {
        var store = new VehicleReportStore();

        store.Record(Report(30, 1));
        store.Record(Report(2, 1));
        store.Record(Report(17, 1));

        CollectionAssert.AreEqual(new[] { 2, 17, 30 }, store.Latest().Select(r => r.VehicleId).ToArray());
    }

    [TestMethod]
    public void RemovedVehiclesNoLongerAppear()
    {
        var store = new VehicleReportStore();
        store.Record(Report(1, 1));
        store.Record(Report(2, 1));

        store.Remove(2);

        Assert.AreEqual(1, store.Latest().Single().VehicleId);
    }

    [TestMethod]
    public void AVehicleThatStopsReportingKeepsItsLastReport()
    {
        var store = new VehicleReportStore();
        store.Record(Report(7, 3));

        // Other vehicles keep reporting while vehicle 7 goes quiet.
        for (long sequence = 1; sequence <= 5; sequence++)
        {
            store.Record(Report(8, sequence));
        }

        VehicleReport silent = store.Latest().Single(r => r.VehicleId == 7);
        Assert.AreEqual(3, silent.Sequence);
        Assert.AreEqual(Instant.AddSeconds(3), silent.ObservedAtUtc);
    }

    private static VehicleReport Report(int vehicleId, long sequence) =>
        new(vehicleId, sequence, Instant.AddSeconds(sequence), -75.34, 41.09, 42, "swiftwater");

    private static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2026-09-09T12:00:00Z");
}
