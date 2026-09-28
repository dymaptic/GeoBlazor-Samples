using FleetDashboard.Client.Rendering;
using FleetDashboard.Client.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class VehicleFeatureIdentityMapTests
{
    [TestMethod]
    public void ReaddedVehiclesKeepAssignedObjectIdsSeparateFromVehicleIds()
    {
        var identities = new VehicleFeatureIdentityMap([1, 2]);

        identities.Remove(1);
        identities.Remove(2);
        identities.Add(1, 51);
        identities.Add(2, 52);
        var updatedGraphic = VehicleGraphicFactory.Create(Vehicle(), objectId: identities.GetObjectId(1));

        Assert.AreEqual(51, identities.GetObjectId(1));
        Assert.AreEqual(52, identities.GetObjectId(2));
        Assert.AreEqual(51, updatedGraphic.Attributes["oid"]);
        Assert.IsTrue(identities.TryGetVehicleId(51, out int selectedVehicleId));
        Assert.AreEqual(1, selectedVehicleId);
    }

    private static VehicleListItem Vehicle()
    {
        return new VehicleListItem(1, "swiftwater", "Swiftwater loop", -75.34, 41.09, 42,
            DateTimeOffset.Parse("2026-09-08T12:00:00Z"), VehicleStatus.Moving, 0);
    }
}
