using NetTopologySuite.Features;
using SpatialDispatch.Data.Entities;

namespace SpatialDispatch.Data;

/// <summary>
///     Turns stored rows into the GeoJSON feature collections the map's layers consume.
/// </summary>
/// <remarks>
///     This lives apart from the endpoints so the layer shape can be tested without a database. The
///     attribute names and value types are a contract with <c>DispatchMap.razor</c>, whose highlight layer
///     filters on <c>TerritoryId</c>; renaming a key here silently stops the territory from highlighting.
/// </remarks>
public static class MapLayerFeatures
{
    public static FeatureCollection ForTerritories(IEnumerable<Territory> territories)
    {
        FeatureCollection collection = [];

        foreach (Territory territory in territories)
        {
            AttributesTable attributes = new()
            {
                { "TerritoryId", territory.Id },
                { "Name", territory.Name }
            };

            collection.Add(new Feature(territory.Boundary, attributes));
        }

        return collection;
    }

    /// <param name="technicians">
    ///     Each technician with its <see cref="Technician.Territory" /> loaded; the territory name is part of
    ///     the layer's attributes.
    /// </param>
    public static FeatureCollection ForTechnicians(IEnumerable<Technician> technicians)
    {
        FeatureCollection collection = [];

        foreach (Technician technician in technicians)
        {
            AttributesTable attributes = new()
            {
                { "TechnicianId", technician.Id },
                { "Name", technician.Name },
                { "TerritoryId", technician.TerritoryId },
                { "TerritoryName", technician.Territory?.Name ?? string.Empty },
                // Written as 0 or 1 rather than true/false: integer fields filter predictably in ArcGIS
                // feature layers, and this matches the static layer files the dashboard shipped with.
                { "IsAvailable", technician.IsAvailable ? 1 : 0 },
                { "LocationLabel", technician.LocationLabel }
            };

            collection.Add(new Feature(technician.Location, attributes));
        }

        return collection;
    }
}
