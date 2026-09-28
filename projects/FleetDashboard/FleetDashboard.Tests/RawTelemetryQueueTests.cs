using FleetDashboard.Client.State;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class RawTelemetryQueueTests
{
    [TestMethod]
    public void CaptureReturnsRecordsInArrivalOrder()
    {
        var queue = new RawTelemetryQueue();
        queue.Accept(Frame(1, [Report(7, 0), Report(8, 0)]));
        queue.Accept(Frame(1, [Report(7, 1), Report(8, 1)]));

        PendingFleetBatch batch = queue.Capture(10);

        CollectionAssert.AreEqual(new long[] { 0, 0, 1, 1 }, batch.Reports.Select(r => r.Sequence).ToArray());
        Assert.HasCount(0, batch.RemovedVehicleIds);
    }

    [TestMethod]
    public void CaptureBoundsTheReturnedBatchAndKeepsTheRemainder()
    {
        var queue = new RawTelemetryQueue();
        queue.Accept(Frame(1, Enumerable.Range(1, 5).Select(id => Report(id, id)).ToArray()));

        PendingFleetBatch first = queue.Capture(2);
        PendingFleetBatch second = queue.Capture(10);

        Assert.HasCount(2, first.Reports);
        Assert.HasCount(3, second.Reports);
        Assert.AreEqual(0, queue.PendingCount);
    }

    [TestMethod]
    public void ReachingTheCapacityRaisesTheSafetyStopInsteadOfDroppingSilently()
    {
        var queue = new RawTelemetryQueue();
        VehicleReport[] overload = Enumerable.Range(1, RawTelemetryQueue.Capacity + 25)
            .Select(id => Report(id % 500, id)).ToArray();

        queue.Accept(Frame(1, overload));

        Assert.IsTrue(queue.SafetyStop);
        Assert.AreEqual(RawTelemetryQueue.Capacity, queue.PendingCount);
        Assert.AreEqual(25, queue.RejectedRecordCount);
    }

    [TestMethod]
    public void TheSafetyStopHaltsEnqueueingUntilReset()
    {
        var queue = new RawTelemetryQueue();
        queue.Accept(Frame(1, Enumerable.Range(1, RawTelemetryQueue.Capacity)
            .Select(id => Report(id % 500, id)).ToArray()));

        queue.Accept(Frame(1, [Report(7, 1), Report(7, 2)]));
        Assert.AreEqual(RawTelemetryQueue.Capacity, queue.PendingCount);
        Assert.AreEqual(2, queue.RejectedRecordCount);

        queue.Reset();
        Assert.IsFalse(queue.SafetyStop);
        Assert.AreEqual(0, queue.PendingCount);
        Assert.AreEqual(0, queue.RejectedRecordCount);
        queue.Accept(Frame(1, [Report(7, 3)]));
        Assert.AreEqual(1, queue.PendingCount);
    }

    [TestMethod]
    public void NullFramesAreRejected()
    {
        var queue = new RawTelemetryQueue();
        Assert.ThrowsExactly<ArgumentNullException>(() => queue.Accept(null!));
    }

    private static FleetFrame Frame(int sequence, VehicleReport[] vehicles) =>
        new(RunId, sequence, Instant.AddMilliseconds(sequence), Array.AsReadOnly(vehicles));

    private static VehicleReport Report(int id, long sequence) =>
        new(id, sequence, Instant.AddMilliseconds(sequence), -75.34, 41.09, 42, "swiftwater");

    private static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2026-09-10T12:00:00Z");
    private static readonly Guid RunId = Guid.Parse("20000000-0000-0000-0000-000000000002");
}
