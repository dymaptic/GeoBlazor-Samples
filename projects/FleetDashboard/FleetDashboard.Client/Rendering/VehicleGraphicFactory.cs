using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Model;
using FleetDashboard.Client.State;


namespace FleetDashboard.Client.Rendering;


public static class VehicleGraphicFactory
{
    public static Graphic Create(VehicleListItem vehicle, Guid? graphicId = null, int? objectId = null)
    {
        return new Graphic(new Point(longitude: vehicle.Longitude, latitude: vehicle.Latitude,
            spatialReference: new SpatialReference(wkid: 4326)),
            attributes: new AttributesDictionary(new Dictionary<string, object?>
            {
                ["oid"] = objectId ?? vehicle.VehicleId,
                ["vehicle"] = $"Truck {vehicle.VehicleId:D3}",
                ["speedKph"] = vehicle.SpeedKph,
                ["route"] = vehicle.RouteName,
                ["status"] = vehicle.Status.ToString()
            }))
        { Id = graphicId ?? Guid.NewGuid() };
    }
}
