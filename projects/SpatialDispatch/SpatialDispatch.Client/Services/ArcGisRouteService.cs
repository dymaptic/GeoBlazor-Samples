using System.Diagnostics.CodeAnalysis;
using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Pro.Components;
using dymaptic.GeoBlazor.Pro.Model;
using dymaptic.GeoBlazor.Pro.Results;
using Microsoft.Extensions.Logging;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;
using GeoBlazorPoint = dymaptic.GeoBlazor.Core.Components.Geometries.Point;

namespace SpatialDispatch.Client.Services;

/// <summary>
///     Draws a real road route by asking the ArcGIS route service, and falls back to the straight connector
///     whenever it cannot.
/// </summary>
/// <remarks>
///     Routing is the one thing in this application that needs a credential and a network round trip, and it
///     is applied after the database has already chosen the technician. So every failure here degrades to
///     <see cref="StraightLineRouteService" /> instead of surfacing: a missing key, an expired key, a
///     blocked conference network, or a service that answers with no route all end up drawing the same
///     dashed line, and the recommendation on screen never changes. Degrading silently is not the same as
///     failing silently, so every one of those failures is logged at warning with its exception: a rejected
///     key and a bug in this file look identical on screen and have to be told apart in the browser console.
/// </remarks>
public sealed class ArcGisRouteService(
    RouteService routeService,
    StraightLineRouteService fallback,
    ILogger<ArcGisRouteService> logger,
    string apiKey,
    string routeUrl) : IRouteService
{
    /// <summary>
    ///     Esri's hosted network-analysis service for the World route layer, which is what an ArcGIS
    ///     location-platform API key grants access to.
    /// </summary>
    /// <remarks>
    ///     Overridable through <c>ArcGis:RouteUrl</c> so that pointing this at ArcGIS Enterprise, or at a
    ///     different Esri endpoint, is a settings change rather than a code change before a talk.
    /// </remarks>
    public const string DefaultRouteUrl =
        "https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World";

    public async Task<RoutePath> GetRouteAsync(
        GeoPoint from,
        GeoPoint to,
        CancellationToken cancellationToken = default)
    {
        RouteSolveResult? result;

        try
        {
            RouteParameters parameters = new()
            {
                ApiKey = apiKey,
                // The service returns coordinates in whatever this asks for. WGS 84 keeps the drawn path in
                // the same reference as every other coordinate in the application, so nothing downstream
                // has to reproject.
                OutSpatialReference = new SpatialReference(DemoContract.Srid),
                ReturnRoutes = true,
                ReturnDirections = false,
                ReturnStops = false,
                // Stops go in as a FeatureSet of graphics, not as the RouteStop collection the property
                // names suggest: RouteParameters.RouteStops is marked obsolete and its setter throws,
                // because of a bug in the underlying ArcGIS JS API. Nothing here needs the stops labeled,
                // since ReturnStops is false and the only thing read back is the route geometry.
                FeatureSetStops = new FeatureSet
                {
                    Features =
                    [
                        new Graphic(ToGeoBlazorPoint(from)),
                        new Graphic(ToGeoBlazorPoint(to))
                    ]
                }
            };

            // Solve takes no cancellation token, so a canceled request is observed here rather than
            // interrupted. The call is one short round trip against a single route, which is why that is
            // acceptable instead of worth wrapping.
            result = await routeService.Solve(routeUrl, parameters);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "The ArcGIS route service did not answer for {RouteUrl}; drawing the straight-line fallback.",
                routeUrl);

            result = null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return TryReadPath(result, out RoutePath? route)
            ? route
            : await fallback.GetRouteAsync(from, to, cancellationToken);
    }

    /// <summary>
    ///     Pulls the drawn path out of a solve result, or reports that there is not one to draw.
    /// </summary>
    /// <remarks>
    ///     Split out from the call above because this is the part that can be wrong without anyone noticing:
    ///     a route service that answers successfully with zero routes, or with a geometry that is not a
    ///     polyline, has to reach the straight-line fallback rather than throw or draw nothing. Tests cover
    ///     these branches directly; the JS interop call itself cannot run outside a browser.
    /// </remarks>
    public static bool TryReadPath(
        RouteSolveResult? result,
        [NotNullWhen(true)] out RoutePath? route)
    {
        route = null;

        if (result?.RouteResults is not { Count: > 0 } routeResults)
        {
            return false;
        }

        if (routeResults[0].Route?.Geometry is not Polyline { Paths: { Count: > 0 } paths })
        {
            return false;
        }

        // A MapPoint is a List<double> whose first two entries are longitude and latitude, in that order.
        List<GeoPoint> points = paths[0]
            .Where(point => point.Count >= 2)
            .Select(point => new GeoPoint(point[0], point[1]))
            .ToList();

        if (points.Count < 2)
        {
            return false;
        }

        route = new RoutePath(points, IsFallback: false);

        return true;
    }

    private static GeoBlazorPoint ToGeoBlazorPoint(GeoPoint position) =>
        new(position.Longitude, position.Latitude, spatialReference: new SpatialReference(DemoContract.Srid));
}
