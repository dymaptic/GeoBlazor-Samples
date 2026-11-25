using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using dymaptic.GeoBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddGeoBlazor(builder.Configuration);

await builder.Build().RunAsync();
