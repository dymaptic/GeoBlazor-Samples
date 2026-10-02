using NetTopologySuite.Geometries;

namespace SpatialDispatch.Data.Entities;

/// <summary>
///     A technician as stored: a current position, a territory assignment, and an availability flag.
/// </summary>
/// <remarks>
///     <see cref="IsAvailable" /> and <see cref="TerritoryId" /> are deliberately plain business columns.
///     The talk's claim rests on them composing with <see cref="Location" /> in one LINQ query, with no
///     spatial-specific query syntax.
/// </remarks>
public sealed class Technician
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int TerritoryId { get; set; }

    public Territory? Territory { get; set; }

    public bool IsAvailable { get; set; }

    /// <summary>A human-readable label for where the technician currently is.</summary>
    public required string LocationLabel { get; set; }

    /// <summary>The technician's current position. Longitude first, SRID 4326.</summary>
    public required Point Location { get; set; }
}
