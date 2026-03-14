using Blazored.LocalStorage;
using dymaptic.GeoBlazor.Pro;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NationFinder.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddGeoBlazorPro(builder.Configuration);
builder.Services.AddScoped<SignalRClient>();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
