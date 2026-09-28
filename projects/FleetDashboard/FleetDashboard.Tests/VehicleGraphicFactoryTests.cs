using FleetDashboard.Client.Rendering;
using FleetDashboard.Client.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class VehicleGraphicFactoryTests
{
    [TestMethod]
    public void GraphicUsesVehicleIdAsTheObjectId()
    {
        var graphic = VehicleGraphicFactory.Create(Vehicle(VehicleStatus.Moving));

        Assert.AreEqual(7, graphic.Attributes["oid"]);
        Assert.AreEqual("Truck 007", graphic.Attributes["vehicle"]);
    }

    [TestMethod]
    public void StaleGraphicCarriesTextStatus()
    {
        var graphic = VehicleGraphicFactory.Create(Vehicle(VehicleStatus.Stale));

        Assert.AreEqual("Stale", graphic.Attributes["status"]);
        Assert.AreEqual("Swiftwater loop", graphic.Attributes["route"]);
    }

    [TestMethod]
    public void StableGraphicIdentityReplacesThePreviousSourceEntry()
    {
        var initial = VehicleGraphicFactory.Create(Vehicle(VehicleStatus.Moving));
        var updated = VehicleGraphicFactory.Create(Vehicle(VehicleStatus.Stale), initial.Id);
        var retained = new[] { initial }.Except([updated]).Concat([updated]).ToArray();

        Assert.HasCount(1, retained);
        Assert.AreEqual("Stale", retained[0].Attributes["status"]);
    }

    private static VehicleListItem Vehicle(VehicleStatus status)
    {
        return new VehicleListItem(7, "swiftwater", "Swiftwater loop", -75.34, 41.09, 42,
            DateTimeOffset.Parse("2026-09-08T12:00:00Z"), status, 0);
    }
}
