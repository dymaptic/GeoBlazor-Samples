using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Pro;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpatialDispatch.Browser;
using SpatialDispatch.Client;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

// The client's router, layout, and pages, unchanged. Without prerendering there is no persisted job list, so
// the board falls through to asking the in-memory service directly.
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Same degrade as the client: with no Pro key the map panel shows GeoBlazor's own message and the dispatch
// answer beside it still works. See SpatialDispatch.Client/Program.cs for why a Core key is not an option.
bool hasProLicense = !string.IsNullOrWhiteSpace(builder.Configuration["GeoBlazor:LicenseKey"]);

if (hasProLicense)
{
    builder.Services.AddGeoBlazorPro(builder.Configuration);
}
else
{
    builder.Services.AddGeoBlazor(builder.Configuration);
}

builder.Services.AddBrowserDispatch(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
