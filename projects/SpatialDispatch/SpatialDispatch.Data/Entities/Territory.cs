using NetTopologySuite.Geometries;

namespace SpatialDispatch.Data.Entities;

/// <summary>
///     A service territory as stored, with its boundary as a SQL Server <c>geography</c> polygon.
/// </summary>
/// <remarks>
///     This is the whole point of the session: <see cref="Boundary" /> is an ordinary property on an
///     ordinary entity, sitting next to <see cref="Name" />. Nothing about the class says "GIS".
/// </remarks>
public sealed class Territory
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>The territory outline. Longitude first, SRID 4326.</summary>
    public required Polygon Boundary { get; set; }

    public ICollection<Technician> Technicians { get; } = [];
}
