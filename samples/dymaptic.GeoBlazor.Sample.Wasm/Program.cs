using dymaptic.GeoBlazor;
using dymaptic.GeoBlazor.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Sample.Wasm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddScoped<LayoutService>();
builder.Services.AddSingleton<ISampleSourceProvider, SampleSourceProvider>();
builder.Configuration.AddInMemoryCollection();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddSingleton<IConfiguration>(_ => builder.Configuration);

await builder.Build().RunAsync();