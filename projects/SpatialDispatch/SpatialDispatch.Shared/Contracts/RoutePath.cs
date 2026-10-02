namespace SpatialDispatch.Shared.Contracts;

/// <summary>
///     A drawn path between the recommended technician and the job.
/// </summary>
/// <param name="Points">Ordered vertices, longitude first.</param>
/// <param name="IsFallback">
///     True when routing was unavailable and the path is the straight connector between the two points.
///     Routing is a visual enrichment applied after the database picks the technician, so a fallback path
///     never changes the recommendation.
/// </param>
public sealed record RoutePath(IReadOnlyList<GeoPoint> Points, bool IsFallback);
