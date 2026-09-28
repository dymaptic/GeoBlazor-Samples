using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FleetDashboard.Client;
using dymaptic.GeoBlazor.Core;
using FleetDashboard.Client.Streaming;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddScoped(sp => new HubConnectionBuilder()
    .WithUrl(new Uri(new Uri(builder.HostEnvironment.BaseAddress), "fleet"))
    // A visible first reconnect delay: the default zero-delay attempt flashes past too fast
    // for the audience to see the "Reconnecting" state before recovery.
    .WithAutomaticReconnect([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
    .Build());
builder.Services.AddScoped<IFleetHubConnection>(sp =>
    new HubConnectionAdapter(sp.GetRequiredService<HubConnection>()));
builder.Services.AddScoped<IFleetFrameSource, SignalRFleetFrameSource>();
builder.Services.AddSingleton(TimeProvider.System);

await builder.Build().RunAsync();
