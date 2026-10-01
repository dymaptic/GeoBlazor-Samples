using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Api;

/// <summary>
///     The dispatch workflow, as ordinary JSON.
/// </summary>
/// <remarks>
///     The other half of the GeoJSON boundary. <see cref="DispatchResult" /> is a business answer that
///     happens to contain coordinates; it is not a map feature, so it is not dressed up as one.
/// </remarks>
public static class DispatchEndpoints
{
    public static IEndpointRouteBuilder MapDispatchEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder api = routes.MapGroup("/api");

        api.MapGet("/jobs", async (IDispatchService dispatch, CancellationToken cancellationToken) =>
            await dispatch.GetOpenJobsAsync(cancellationToken));

        api.MapGet("/dispatch/{jobId:int}", async Task<IResult> (
            int jobId,
            IDispatchService dispatch,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return TypedResults.Ok(await dispatch.GetDispatchAsync(jobId, cancellationToken));
            }
            catch (DispatchUnavailableException ex)
            {
                // A job outside every territory, or a territory with nobody available, is a data state
                // rather than a server fault. The dashboard shows the message, so it has to be readable.
                // Anything else - a translation failure, a dropped connection - stays a 500 on purpose.
                return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        });

        return routes;
    }
}
