using NetTopologySuite.Geometries;
using SpatialDispatch.Shared.Contracts;

namespace SpatialDispatch.Data.Entities;

/// <summary>
///     An open service job as stored. The customer address was geocoded before this workflow, so the
///     location arrives as a coordinate rather than as text to be resolved.
/// </summary>
/// <remarks>
///     There is no status column. Assignment is presentation state in the browser and is never persisted,
///     which is what lets a page refresh reset the demo between rehearsals.
/// </remarks>
public sealed class Job
{
    public int Id { get; set; }

    public required string Customer { get; set; }

    public required string Summary { get; set; }

    public JobPriority Priority { get; set; }

    /// <summary>The recognizable public place the coordinate sits at, shown to orient the audience.</summary>
    public required string MapAnchor { get; set; }

    /// <summary>The customer's position. Longitude first, SRID 4326.</summary>
    public required Point Location { get; set; }
}
