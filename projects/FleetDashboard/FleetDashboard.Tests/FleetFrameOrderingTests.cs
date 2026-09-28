using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FleetDashboard.Tests;

[TestClass]
public sealed class FleetFrameOrderingTests
{
    [TestMethod]
    public void KeepsTheNewerLiveFrameWhenAnOlderSnapshotArrives()
    {
        Guid runId = Guid.Parse("baddad00-0000-0000-0000-000000000007");
        FleetFrame live = Frame(runId, 8);
        FleetFrame snapshot = Frame(runId, 7);

        FleetFrame selected = FleetFrameOrdering.Newest(live, snapshot);

        Assert.AreSame(live, selected);
    }

    [TestMethod]
    public void ReconcilesASnapshotWithANewerSameRunLiveFrame()
    {
        Guid runId = Guid.Parse("baddad00-0000-0000-0000-000000000007");
        FleetFrame snapshot = Frame(runId, 7);
        FleetFrame live = Frame(runId, 8);

        FleetFrame selected = FleetFrameOrdering.ReconcileSnapshot(snapshot, live, null);

        Assert.AreSame(live, selected);
    }

    private static FleetFrame Frame(Guid runId, long sequence) =>
        new(runId, sequence, DateTimeOffset.Parse("2026-09-08T12:00:00Z"), []);
}
