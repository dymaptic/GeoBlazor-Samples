using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Pro;
using dymaptic.GeoBlazor.Pro.Sample.Wasm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Configuration.AddInMemoryCollection();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IConfiguration>(_ => builder.Configuration);
builder.Services.AddScoped<HttpClient>();

builder.Services.AddGeoBlazorPro(builder.Configuration);
builder.Services.AddScoped<LayoutService>();

await builder.Build().RunAsync();