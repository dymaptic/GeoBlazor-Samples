using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Shared.Demo;

/// <summary>
///     The no-credentials routing path: a straight connector between the technician and the job.
/// </summary>
/// <remarks>
///     This is the behavior an attendee gets from a cold clone, and the behavior on stage if the routing
///     service is unreachable. It is a deliberate fallback rather than an error, because the recommendation
///     is already settled by the time anything is drawn.
/// </remarks>
public sealed class StraightLineRouteService : IRouteService
{
    public Task<RoutePath> GetRouteAsync(
        GeoPoint from,
        GeoPoint to,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RoutePath([from, to], IsFallback: true));
}
