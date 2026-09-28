using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using FleetDashboard.Shared.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class DeterministicFleetFrameSourceTests
{
    [TestMethod]
    public async Task ResetRestoresStartingPositionsWithANewRun()
    {
        var source = new DeterministicFleetFrameSource();
        DateTimeOffset instant = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        FleetFrame first = await source.GetFrameAsync(instant);
        await source.GetFrameAsync(instant.AddMinutes(1));
        source.Reset();
        FleetFrame reset = await source.GetFrameAsync(instant.AddMinutes(2));

        Assert.AreNotEqual(first.RunId, reset.RunId);
        Assert.AreEqual(1, reset.FrameSequence);
        // Vehicle 7 travels the first catalogued route and starts at its first road vertex
        // when simulation time is zero.
        RoutePoint start = FleetRoutes.All[(7 + 1) % FleetRoutes.All.Count].Points[0];
        Assert.AreEqual(start.Longitude, reset.Vehicles[6].Longitude, 0.000001);
        Assert.AreEqual(start.Latitude, reset.Vehicles[6].Latitude, 0.000001);
    }

    [TestMethod]
    public async Task SuccessiveFramesKeepVehicleIdentityAndAdvanceSequence()
    {
        var source = new DeterministicFleetFrameSource(1190232);
        DateTimeOffset instant = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        FleetFrame first = await source.GetFrameAsync(instant);
        FleetFrame next = await source.GetFrameAsync(instant.AddSeconds(1));

        CollectionAssert.AreEqual(first.Vehicles.Select(v => v.VehicleId).ToArray(),
            next.Vehicles.Select(v => v.VehicleId).ToArray());
        Assert.AreEqual(2, next.FrameSequence);
        Assert.IsTrue(next.Vehicles.All(v => v.Sequence == 2));
        Assert.AreNotEqual(first.Vehicles[6].Latitude, next.Vehicles[6].Latitude);
        Assert.HasCount(4, next.Vehicles.Select(v => v.RouteId).Distinct().ToArray());
    }

    [TestMethod]
    public async Task SameSeedProducesTheSameFirstFrame()
    {
        var first = new DeterministicFleetFrameSource(1190232);
        var second = new DeterministicFleetFrameSource(1190232);
        DateTimeOffset instant = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        FleetFrame frame = await first.GetFrameAsync(instant);
        FleetFrame repeated = await second.GetFrameAsync(instant);

        CollectionAssert.AreEqual(frame.Vehicles.ToArray(), repeated.Vehicles.ToArray());
        Assert.HasCount(50, frame.Vehicles);
        VehicleReport truck = frame.Vehicles.Single(v => v.VehicleId == 7);
        Assert.AreEqual("swiftwater", truck.RouteId);
        Assert.AreEqual(42, truck.SpeedKph);
        RoutePoint start = FleetRoutes.All[0].Points[0];
        Assert.AreEqual(start.Longitude, truck.Longitude, 0.000001);
        Assert.AreEqual(start.Latitude, truck.Latitude, 0.000001);
    }
}
