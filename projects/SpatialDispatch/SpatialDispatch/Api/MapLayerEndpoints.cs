using Microsoft.EntityFrameworkCore;
using SpatialDispatch.Data;
using SpatialDispatch.Data.Entities;

namespace SpatialDispatch.Api;

/// <summary>
///     The map's feature layers, as GeoJSON.
/// </summary>
/// <remarks>
///     One half of a boundary the session states out loud: GeoJSON carries map features, and ordinary DTOs
///     carry business results. Mixing them produces an API that is awkward for both.
/// </remarks>
public static class MapLayerEndpoints
{
    public static IEndpointRouteBuilder MapMapLayerEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder layers = routes.MapGroup("/api/layers");

        layers.MapGet("/territories", async (DispatchDbContext db, CancellationToken cancellationToken) =>
        {
            List<Territory> territories = await db.Territories
                .OrderBy(t => t.Id)
                .ToListAsync(cancellationToken);

            return MapLayerFeatures.ForTerritories(territories);
        });

        layers.MapGet("/technicians", async (DispatchDbContext db, CancellationToken cancellationToken) =>
        {
            List<Technician> technicians = await db.Technicians
                .Include(t => t.Territory)
                .OrderBy(t => t.Id)
                .ToListAsync(cancellationToken);

            return MapLayerFeatures.ForTechnicians(technicians);
        });

        return routes;
    }
}
