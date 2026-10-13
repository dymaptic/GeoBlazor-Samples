using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Browser;

/// <summary>
///     Registers the services that let the dispatch board run with no server behind it.
/// </summary>
/// <remarks>
///     These are the same three seams the server-hosted app fills with SQL Server and its API. Here the
///     answer comes from the in-memory demo contract, the layers from the GeoJSON files shipped in
///     <c>wwwroot/data</c>, and the route from the straight-line connector, so a static file host is enough.
/// </remarks>
public static class BrowserServiceRegistration
{
    public static IServiceCollection AddBrowserDispatch(this IServiceCollection services, string baseAddress)
    {
        services.AddScoped<IDispatchService, InMemoryDispatchService>();
        services.AddScoped<IMapLayerSource>(_ => new StaticFileMapLayerSource(baseAddress));
        services.AddScoped<IRouteService, StraightLineRouteService>();

        return services;
    }
}
