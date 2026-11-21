using dymaptic.Blazor.StateManagement;
using dymaptic.GeoBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ShipmentTracker.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddClientStateManagement([
    typeof(Shipment),
    typeof(PickupRequestModel),
    typeof(MonthlyStat),
    typeof(CompanySettings)
], 1);
await builder.Build().RunAsync();
