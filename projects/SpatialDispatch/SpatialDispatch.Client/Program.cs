using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Pro;
using dymaptic.GeoBlazor.Pro.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpatialDispatch.Client.Services;
using SpatialDispatch.Shared.Api;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// This application needs a GeoBlazor Pro license key. A free Core registration key does not work here:
// the project references the Pro package, and a Core key is only accepted by a project that references
// dymaptic.GeoBlazor.Core directly, so offering it as a cheaper option would send an attendee down a path
// that cannot succeed without editing the csproj.
//
// The else branch is therefore a graceful degrade rather than a second supported configuration. With no
// license key, Core's validator throws, the ErrorBoundary catches it, and the map panel shows GeoBlazor's
// own message asking for a key. The dispatch answer beside it is unaffected, which is the property worth
// keeping: a key problem on the day costs the map and nothing else.
bool hasProLicense = !string.IsNullOrWhiteSpace(builder.Configuration["GeoBlazor:LicenseKey"]);

if (hasProLicense)
{
    builder.Services.AddGeoBlazorPro(builder.Configuration);
}
else
{
    builder.Services.AddGeoBlazor(builder.Configuration);
}

// The Task 3 swap, in full. The dispatch answer now comes from SQL Server spatial queries and the map's
// layers now come from the GeoJSON endpoints; not one component changed to make that true.
builder.Services.AddScoped<IDispatchService, ApiDispatchService>();
builder.Services.AddScoped<IMapLayerSource>(_ =>
    new ApiMapLayerSource(builder.HostEnvironment.BaseAddress));

// Routing is drawn after the database has already chosen the technician, so it never participates in the
// recommendation and its absence costs the demo nothing but a dashed line.
builder.Services.AddScoped<StraightLineRouteService>();

// GeoBlazor's documented convention is a top-level ArcGISApiKey, shared by the map and this route
// service. A nested ArcGis:ApiKey is still honored as a fallback for anyone who configured the app
// before this matched GeoBlazor.
string? arcGisApiKey = builder.Configuration["ArcGISApiKey"]
    ?? builder.Configuration["ArcGis:ApiKey"];

if (hasProLicense && !string.IsNullOrWhiteSpace(arcGisApiKey))
{
    string routeUrl = builder.Configuration["ArcGis:RouteUrl"] ?? ArcGisRouteService.DefaultRouteUrl;

    builder.Services.AddScoped<IRouteService>(services => new ArcGisRouteService(
        services.GetRequiredService<RouteService>(),
        services.GetRequiredService<StraightLineRouteService>(),
        services.GetRequiredService<ILogger<ArcGisRouteService>>(),
        arcGisApiKey,
        routeUrl));
}
else
{
    builder.Services.AddScoped<IRouteService>(
        services => services.GetRequiredService<StraightLineRouteService>());
}

await builder.Build().RunAsync();
