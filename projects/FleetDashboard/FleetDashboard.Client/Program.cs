using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FleetDashboard.Client;
using dymaptic.GeoBlazor.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddFleetFrameSource(builder.Configuration, new Uri(builder.HostEnvironment.BaseAddress));
builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
