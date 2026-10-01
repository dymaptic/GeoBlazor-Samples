using SpatialDispatch.Shared.Contracts;

namespace SpatialDispatch.Shared.Services;

/// <summary>
///     Draws the path from the recommended technician to the job.
/// </summary>
/// <remarks>
///     Implementations return a straight connector rather than throwing when routing is unavailable, because
///     the application must stay usable with no ArcGIS API key configured.
/// </remarks>
public interface IRouteService
{
    Task<RoutePath> GetRouteAsync(GeoPoint from, GeoPoint to, CancellationToken cancellationToken = default);
}
