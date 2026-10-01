using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Pro.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using NSubstitute.Core;
using SpatialDispatch.Client.Services;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;

namespace SpatialDispatch.Tests;

/// <summary>
///     Covers the ways a routing call can succeed at the protocol level and still have no route to draw, and
///     the one way it can fail to answer at all.
/// </summary>
/// <remarks>
///     Running the real ArcGIS route service still needs a browser, but a failure to reach it does not: that
///     always starts with <c>RouteService</c> asking <see cref="IJSRuntime" /> to import its JS module, so a
///     substituted <see cref="IJSRuntime" /> that throws on that call reproduces a blocked network or a
///     rejected key without one. Everything else here is the part that decides between a drawn road route and
///     the straight-line fallback: every branch that returns false has to reach the fallback, since on stage
///     an empty answer must look like the no-credentials path rather than an error or a missing line.
/// </remarks>
public class ArcGisRouteServiceTests
{
    [Fact]
    public async Task A_solve_failure_logs_a_warning_and_falls_back()
    {
        IJSRuntime jsRuntime = SubstituteJsRuntimeThatCannotImportItsModule();
        ILogger<ArcGisRouteService> logger = Substitute.For<ILogger<ArcGisRouteService>>();

        ArcGisRouteService service = new(
            RealRouteServiceOver(jsRuntime),
            new StraightLineRouteService(),
            logger,
            apiKey: "unused-in-this-test",
            routeUrl: "https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World");

        RoutePath route = await service.GetRouteAsync(
            new GeoPoint(-75.671329, 41.410730),
            new GeoPoint(-75.683967, 41.360406),
            TestContext.Current.CancellationToken);

        // The dashboard prints "Routing is not configured" whenever this is true, so a failed call has to
        // draw the same dashed line the no-credentials path draws.
        Assert.True(route.IsFallback);
        Assert.True(LoggedWarningFor<JSException>(logger), "Expected a warning logged with the JSException.");
    }

    [Fact]
    public void No_result_at_all_has_no_path()
    {
        Assert.False(ArcGisRouteService.TryReadPath(null, out RoutePath? route));
        Assert.Null(route);
    }

    [Fact]
    public void A_successful_answer_carrying_no_routes_has_no_path()
    {
        Assert.False(ArcGisRouteService.TryReadPath(SolveResult([]), out RoutePath? route));
        Assert.Null(route);
    }

    [Fact]
    public void A_route_whose_geometry_is_not_a_polyline_has_no_path()
    {
        RouteSolveResult result = SolveResult([RouteResult(new Graphic(new Point(-75.68, 41.36)))]);

        Assert.False(ArcGisRouteService.TryReadPath(result, out RoutePath? route));
        Assert.Null(route);
    }

    [Fact]
    public void A_single_vertex_is_not_a_drawable_path()
    {
        RouteSolveResult result = SolveResult([RouteResult(Polyline([(-75.671329, 41.410730)]))]);

        Assert.False(ArcGisRouteService.TryReadPath(result, out RoutePath? route));
        Assert.Null(route);
    }

    [Fact]
    public void A_road_route_is_read_longitude_first_and_is_not_a_fallback()
    {
        RouteSolveResult result = SolveResult([
            RouteResult(Polyline([
                (-75.671329, 41.410730),
                (-75.678000, 41.385000),
                (-75.683967, 41.360406)
            ]))
        ]);

        Assert.True(ArcGisRouteService.TryReadPath(result, out RoutePath? route));

        // A reversed pair here would put the drawn route on the East Antarctic ice sheet while the
        // recommendation card beside it stayed correct, which is the failure this whole session warns about.
        Assert.Equal(3, route!.Points.Count);
        Assert.Equal(-75.671329, route.Points[0].Longitude, 6);
        Assert.Equal(41.410730, route.Points[0].Latitude, 6);
        Assert.Equal(-75.683967, route.Points[2].Longitude, 6);
        Assert.Equal(41.360406, route.Points[2].Latitude, 6);

        // The dashboard prints "Routing is not configured" whenever this is true, so a real route that
        // reported itself as a fallback would put a wrong caption under a correct map.
        Assert.False(route.IsFallback);
    }

    /// <summary>
    ///     An <see cref="IJSRuntime" /> that throws on every module import, standing in for a route service
    ///     that cannot be reached.
    /// </summary>
    private static IJSRuntime SubstituteJsRuntimeThatCannotImportItsModule()
    {
        IJSRuntime jsRuntime = Substitute.For<IJSRuntime>();

        // RouteService.Solve reaches its JS module through both of IJSRuntime's generic InvokeAsync
        // overloads depending on whether a cancellation token is supplied, so both need to fail.
        jsRuntime
            .InvokeAsync<IJSObjectReference>(Arg.Any<string>(), Arg.Any<object?[]>())
            .Returns<ValueTask<IJSObjectReference>>(_ => throw new JSException("The route service did not answer."));
        jsRuntime
            .InvokeAsync<IJSObjectReference>(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<object?[]>())
            .Returns<ValueTask<IJSObjectReference>>(_ => throw new JSException("The route service did not answer."));

        return jsRuntime;
    }

    /// <summary>
    ///     A real <c>RouteService</c> over the given <see cref="IJSRuntime" />, built by hand because its
    ///     <c>Solve</c> method is concrete and non-virtual and so cannot be substituted directly.
    /// </summary>
    private static dymaptic.GeoBlazor.Pro.Components.RouteService RealRouteServiceOver(IJSRuntime jsRuntime)
    {
        JsModuleManager jsModuleManager = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        AuthenticationManager authenticationManager = new(jsRuntime, jsModuleManager, configuration);

        return new dymaptic.GeoBlazor.Pro.Components.RouteService(
            authenticationManager,
            jsModuleManager,
            Substitute.For<IAppValidator>(),
            jsRuntime);
    }

    /// <summary>Whether the logger recorded a warning carrying an exception of type <typeparamref name="T"/>.</summary>
    private static bool LoggedWarningFor<T>(ILogger logger) where T : Exception =>
        logger.ReceivedCalls().Any(call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log)
            && call.GetArguments() is [LogLevel.Warning, _, _, T, _]);

    private static Polyline Polyline(IEnumerable<(double Longitude, double Latitude)> vertices) =>
        new([new MapPath(vertices.Select(v => new MapPoint(v.Longitude, v.Latitude)).ToArray())]);

    private static RouteResult RouteResult(Geometry geometry) =>
        RouteResult(new Graphic(geometry));

    private static RouteResult RouteResult(Graphic route) =>
        new(null!, null!, null!, route, "Route", [], null!, null!, null!);

    private static RouteSolveResult SolveResult(IReadOnlyList<RouteResult> routeResults) =>
        new([], [], [], [], routeResults);
}
