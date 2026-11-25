using Blazored.LocalStorage;
using dymaptic.GeoBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NationFinder.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddScoped<SignalRClient>();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
