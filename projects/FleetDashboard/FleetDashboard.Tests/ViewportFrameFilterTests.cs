using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class ViewportFrameFilterTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-09T12:00:00Z");
    private static readonly Guid RunId = Guid.NewGuid();

    // Vehicles 1-3 sit inside the extent; 4-20 sit outside it.
    private static readonly ViewportExtent View = new(-75.40, 41.00, -75.30, 41.10);

    [TestMethod]
    public void TheFirstFrameGoesOutWholeSoTheClientHasTheRoster()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);

        FleetFrame first = filter.Filter(Frame(1, Start));

        Assert.IsFalse(first.IsPartial);
        Assert.AreEqual(20, first.Vehicles.Count);
    }

    [TestMethod]
    public void InViewVehiclesArriveEveryFrame()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);
        _ = filter.Filter(Frame(1, Start));

        for (int tick = 1; tick <= 30; tick++)
        {
            FleetFrame frame = filter.Filter(Frame(tick + 1, Start.AddMilliseconds(100 * tick)));
            Assert.IsTrue(frame.IsPartial);
            CollectionAssert.IsSubsetOf(new[] { 1, 2, 3 }, Ids(frame));
        }
    }

    [TestMethod]
    public void TheSelectedVehicleArrivesEveryFrameWhereverItIs()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);
        filter.SetSelectedVehicle(15);
        _ = filter.Filter(Frame(1, Start));

        for (int tick = 1; tick <= 30; tick++)
        {
            FleetFrame frame = filter.Filter(Frame(tick + 1, Start.AddMilliseconds(100 * tick)));
            CollectionAssert.Contains(Ids(frame), 15);
        }
    }

    [TestMethod]
    public void OutOfViewVehiclesArriveAboutOncePerSecondAndNeverMoreOften()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);
        _ = filter.Filter(Frame(1, Start));
        Dictionary<int, List<DateTimeOffset>> sends = [];

        for (int tick = 1; tick <= 50; tick++)
        {
            DateTimeOffset at = Start.AddMilliseconds(100 * tick);
            foreach (int id in Ids(filter.Filter(Frame(tick + 1, at))).Where(id => id > 3))
            {
                if (!sends.TryGetValue(id, out List<DateTimeOffset>? times)) sends[id] = times = [Start];
                times.Add(at);
            }
        }

        Assert.AreEqual(17, sends.Count, "Every out-of-view vehicle must keep arriving.");
        foreach ((int id, List<DateTimeOffset> times) in sends)
        {
            TimeSpan[] gaps = times.Zip(times.Skip(1), (earlier, later) => later - earlier).ToArray();
            Assert.IsTrue(gaps.All(gap => gap >= TimeSpan.FromSeconds(1)), $"Vehicle {id} was sent twice within a second.");
            // The first gap after the full frame is staggered up to 1.9 s; after that it holds at 1 s.
            Assert.IsTrue(gaps.Skip(1).All(gap => gap <= TimeSpan.FromSeconds(1.1)), $"Vehicle {id} went quiet.");
        }
    }

    [TestMethod]
    public void OutOfViewSendsAreSpreadAcrossTheSecond()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);
        _ = filter.Filter(Frame(1, Start));

        int[] outOfViewPerFrame = Enumerable.Range(1, 30)
            .Select(tick => Ids(filter.Filter(Frame(tick + 1, Start.AddMilliseconds(100 * tick)))).Count(id => id > 3))
            .ToArray();

        // 17 out-of-view vehicles over 10 frames per second: about two per frame, never all at once.
        Assert.IsTrue(outOfViewPerFrame.Max() <= 2, string.Join(",", outOfViewPerFrame));
    }

    [TestMethod]
    public void FilteredReportsKeepTheirRealObservationTime()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);
        _ = filter.Filter(Frame(1, Start));
        FleetFrame source = Frame(12, Start.AddSeconds(1.1));

        FleetFrame filtered = filter.Filter(source);

        Assert.IsTrue(filtered.Vehicles.Count > 3);
        foreach (VehicleReport report in filtered.Vehicles)
        {
            Assert.AreSame(source.Vehicles.Single(v => v.VehicleId == report.VehicleId), report);
        }
        Assert.AreEqual(source.RunId, filtered.RunId);
        Assert.AreEqual(source.FrameSequence, filtered.FrameSequence);
        Assert.AreEqual(source.PublishedAtUtc, filtered.PublishedAtUtc);
    }

    [TestMethod]
    public void ARosterChangeGoesOutWholeSoRemovalsReachTheClient()
    {
        var filter = new ViewportFrameFilter();
        filter.SetExtent(View);
        _ = filter.Filter(Frame(1, Start));
        _ = filter.Filter(Frame(2, Start.AddMilliseconds(100)));

        FleetFrame shrunk = filter.Filter(Frame(3, Start.AddMilliseconds(200), vehicleCount: 10));

        Assert.IsFalse(shrunk.IsPartial);
        Assert.AreEqual(10, shrunk.Vehicles.Count);
    }

    [TestMethod]
    public void UnsubscribedConnectionsAreForgotten()
    {
        var subscriptions = new ViewportSubscriptions();
        subscriptions.GetOrAdd("a").SetExtent(View);

        subscriptions.Remove("a");

        Assert.AreEqual(0, subscriptions.Snapshot().Count);
    }

    private static FleetFrame Frame(long sequence, DateTimeOffset publishedAtUtc, int vehicleCount = 20)
    {
        VehicleReport[] reports = Enumerable.Range(1, vehicleCount).Select(id => new VehicleReport(id, sequence,
            publishedAtUtc, id <= 3 ? -75.35 : -75.0, 41.05, 40, "swiftwater")).ToArray();
        return new FleetFrame(RunId, sequence, publishedAtUtc, reports);
    }

    private static int[] Ids(FleetFrame frame) => frame.Vehicles.Select(v => v.VehicleId).ToArray();
}
