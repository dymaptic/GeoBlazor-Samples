using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Configuration.AddInMemoryCollection();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddSingleton<IConfiguration>(_ => builder.Configuration);
builder.Services.AddScoped<LayoutService>();
builder.Services.AddSingleton<ISampleSourceProvider, SampleSourceProvider>();

await builder.Build().RunAsync();