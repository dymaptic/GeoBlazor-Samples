using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;
using JobEntity = SpatialDispatch.Data.Entities.Job;
using TechnicianEntity = SpatialDispatch.Data.Entities.Technician;
using TerritoryEntity = SpatialDispatch.Data.Entities.Territory;

namespace SpatialDispatch.Data;

/// <summary>
///     Puts the records from <c>docs/demo-contract.md</c> into the database.
/// </summary>
/// <remarks>
///     The seed reads <see cref="DemoContract" /> rather than restating the coordinates, so the document has
///     exactly one representation in code and the database cannot drift from the slides. Seeding is skipped
///     only when the tables already hold the same records, field for field, as the contract, so a second
///     <c>dotnet run</c> before a talk is harmless while a database left over from an older contract is
///     rebuilt rather than kept.
/// </remarks>
public static class DemoDataSeeder
{
    /// <inheritdoc cref="DemoContract.Srid" />
    public const int Srid = DemoContract.Srid;

    /// <summary>
    ///     Geometry services whose default spatial reference is WGS 84, so everything they build carries
    ///     SRID 4326 without a per-object assignment that could be forgotten.
    /// </summary>
    private static readonly NtsGeometryServices GeometryServices = new(
        CoordinateArraySequenceFactory.Instance,
        new PrecisionModel(PrecisionModels.Floating),
        Srid);

    private static readonly GeometryFactory Geometry = GeometryServices.CreateGeometryFactory();

    public static async Task SeedAsync(DispatchDbContext db, CancellationToken cancellationToken = default)
    {
        if (await MatchesContractAsync(db, cancellationToken))
        {
            return;
        }

        // Anything else is a database from an older demo contract: it took the migration but kept the rows
        // it was seeded with, and leaving them would put a stale technician list on stage. There is no
        // explicit transaction around the delete and the insert because the host enables retry on failure,
        // and EF Core rejects a user-initiated transaction under a retrying execution strategy. A crash
        // between the two leaves a count that does not match the contract, which is what makes the next
        // run seed again.
        await db.Technicians.ExecuteDeleteAsync(cancellationToken);
        await db.Jobs.ExecuteDeleteAsync(cancellationToken);
        await db.Territories.ExecuteDeleteAsync(cancellationToken);

        db.Territories.AddRange(DemoContract.Territories.Select(t => new TerritoryEntity
        {
            Id = t.Id,
            Name = t.Name,
            Boundary = BoundaryFor(t.Id)
        }));

        db.Technicians.AddRange(DemoContract.Technicians.Select(t => new TechnicianEntity
        {
            Id = t.Id,
            Name = t.Name,
            TerritoryId = t.TerritoryId,
            IsAvailable = t.IsAvailable,
            LocationLabel = t.LocationLabel,
            Location = ToPoint(t.Location)
        }));

        db.Jobs.AddRange(DemoContract.Jobs.Select(j => new JobEntity
        {
            Id = j.Id,
            Customer = j.Customer,
            Summary = j.Summary,
            Priority = j.Priority,
            MapAnchor = j.MapAnchor,
            Location = ToPoint(j.Location)
        }));

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    ///     Whether the database already holds the same records, field for field, as the demo contract
    ///     describes.
    /// </summary>
    /// <remarks>
    ///     Comparing the content rather than only the row counts is what keeps a database from an earlier
    ///     task out of the demo: editing a coordinate or a name in the contract without changing how many
    ///     rows there are still changes the values compared here, and the seed notices. A count-only check
    ///     would skip right over that edit and leave the old value on stage.
    /// </remarks>
    private static async Task<bool> MatchesContractAsync(
        DispatchDbContext db,
        CancellationToken cancellationToken)
    {
        List<TerritoryEntity> territories =
            await db.Territories.AsNoTracking().OrderBy(t => t.Id).ToListAsync(cancellationToken);
        List<TechnicianEntity> technicians =
            await db.Technicians.AsNoTracking().OrderBy(t => t.Id).ToListAsync(cancellationToken);
        List<JobEntity> jobs =
            await db.Jobs.AsNoTracking().OrderBy(j => j.Id).ToListAsync(cancellationToken);

        return territories
                .Select(t => (t.Id, t.Name, Boundary: t.Boundary.AsText()))
                .SequenceEqual(DemoContract.Territories
                    .Select(t => (t.Id, t.Name, Boundary: BoundaryFor(t.Id).AsText())))
            && technicians
                .Select(t => (t.Id, t.Name, t.TerritoryId, t.IsAvailable, t.LocationLabel,
                    Longitude: t.Location.X, Latitude: t.Location.Y))
                .SequenceEqual(DemoContract.Technicians
                    .Select(t => (t.Id, t.Name, t.TerritoryId, t.IsAvailable, t.LocationLabel,
                        Longitude: t.Location.Longitude, Latitude: t.Location.Latitude)))
            && jobs
                .Select(j => (j.Id, j.Customer, j.Summary, j.Priority, j.MapAnchor,
                    Longitude: j.Location.X, Latitude: j.Location.Y))
                .SequenceEqual(DemoContract.Jobs
                    .Select(j => (j.Id, j.Customer, j.Summary, j.Priority, j.MapAnchor,
                        Longitude: j.Location.Longitude, Latitude: j.Location.Latitude)));
    }

    /// <summary>
    ///     Builds a point in the order this session repeats twice: longitude, then latitude.
    /// </summary>
    /// <remarks>
    ///     Reversing these two arguments is the single most common first-timer mistake in spatial work, and
    ///     it fails silently — the row saves, and the technician turns up in the Indian Ocean.
    /// </remarks>
    public static Point ToPoint(GeoPoint position) =>
        Geometry.CreatePoint(new Coordinate(position.Longitude, position.Latitude));

    /// <summary>
    ///     The stored boundary for one territory, parsed from the Well-Known Text in the demo contract.
    /// </summary>
    public static Polygon BoundaryFor(int territoryId)
    {
        WKTReader reader = new(GeometryServices);

        return ReadPolygon(reader, DemoContract.BoundaryWktByTerritoryId[territoryId]);
    }

    private static Polygon ReadPolygon(WKTReader reader, string wkt)
    {
        if (reader.Read(wkt) is not Polygon polygon)
        {
            throw new InvalidOperationException($"Expected a POLYGON but the demo contract had: {wkt}");
        }

        // A geography operand with the wrong SRID makes STContains and STDistance return NULL instead of
        // failing, so an unset SRID here would surface much later as an empty answer on stage.
        if (polygon.SRID != Srid)
        {
            throw new InvalidOperationException(
                $"Boundary polygons must carry SRID {Srid} but this one carried {polygon.SRID}.");
        }

        return polygon;
    }
}
