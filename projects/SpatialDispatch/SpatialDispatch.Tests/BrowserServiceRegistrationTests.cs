using Microsoft.Extensions.DependencyInjection;
using SpatialDispatch.Browser;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Tests;

/// <summary>
///     Holds the browser-only host to the services that let it run without a server.
/// </summary>
/// <remarks>
///     Every one of these has a server-backed counterpart in the client. Resolving the wrong one would not
///     fail the build; it would fail on a static host as a request to an API that is not there.
/// </remarks>
public class BrowserServiceRegistrationTests
{
    private const string BaseAddress = "https://example.test/samples/spatial-dispatch/";

    [Fact]
    public void Browser_host_resolves_the_in_browser_services()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddBrowserDispatch(BaseAddress)
            .BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<InMemoryDispatchService>(scope.ServiceProvider.GetRequiredService<IDispatchService>());
        Assert.IsType<StaticFileMapLayerSource>(scope.ServiceProvider.GetRequiredService<IMapLayerSource>());
        Assert.IsType<StraightLineRouteService>(scope.ServiceProvider.GetRequiredService<IRouteService>());
    }

    [Fact]
    public void Map_layers_are_read_from_under_the_published_sub_path()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddBrowserDispatch(BaseAddress)
            .BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IMapLayerSource layers = scope.ServiceProvider.GetRequiredService<IMapLayerSource>();

        Assert.Equal($"{BaseAddress}data/territories.geojson", layers.TerritoriesUrl);
        Assert.Equal($"{BaseAddress}data/technicians.geojson", layers.TechniciansUrl);
    }
}
