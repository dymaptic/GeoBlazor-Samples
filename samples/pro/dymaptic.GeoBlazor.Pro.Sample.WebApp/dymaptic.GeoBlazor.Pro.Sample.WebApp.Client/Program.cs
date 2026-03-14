using dymaptic.GeoBlazor.Pro;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration.AddInMemoryCollection();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddGeoBlazorPro(builder.Configuration);
builder.Services.AddSingleton<IConfiguration>(_ => builder.Configuration);

await builder.Build().RunAsync();