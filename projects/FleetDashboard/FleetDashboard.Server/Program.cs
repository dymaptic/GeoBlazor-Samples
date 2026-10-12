using FleetDashboard.Server;
using FleetDashboard.Server.Simulation;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Server.Telemetry;
using FleetDashboard.Shared;

// Hosts the WebAssembly dashboard, the simulated fleet, and the SignalR hub in one process.
// Two independent loops run here: the simulator writes truck reports into the report store on
// its own clock, and the broadcaster builds a frame from the store and publishes it on another.

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(ScenarioSettings.Normal);
builder.Services.AddSingleton<VehicleReportStore>();
builder.Services.AddSingleton<FleetSimulator>();
builder.Services.AddSingleton<FleetFrameBuilder>();
builder.Services.AddSingleton<ViewportSubscriptions>();
builder.Services.AddSingleton<FleetBroadcaster>();

// The hub and the host share one instance of each loop. The simulator is registered first, so its
// constructor fills the store before the broadcaster builds the first frame.
builder.Services.AddHostedService(sp => sp.GetRequiredService<FleetSimulator>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<FleetBroadcaster>());

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();

    // The WebAssembly runtime reads its environment from this response header and uses it
    // to decide whether to fetch appsettings.Development.json. UseBlazorFrameworkFiles()
    // normally sets it, and removing that call (see the note below) took the header with
    // it, leaving the client running as Production and silently ignoring the local
    // registration key. Set it directly instead of bringing the middleware back.
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/_framework"))
        {
            context.Response.Headers.Append("Blazor-Environment", app.Environment.EnvironmentName);
        }

        await next(context);
    });
}

// MapStaticAssets serves the referenced client's wwwroot AND its _framework output,
// including the mapping from the plain "blazor.webassembly.js" that index.html asks
// for to the fingerprinted file .NET 10 actually emits.
//
// Do not add UseBlazorFrameworkFiles() alongside it. That is the pre-.NET 9 way to
// serve _framework, and its static-file branch short-circuits the request after
// routing has already picked an endpoint, so every framework file 500s with
// "reached the end of the pipeline without executing the endpoint".
app.MapStaticAssets();

app.MapHub<FleetHub>("/fleet");

app.MapFallbackToFile("index.html");

app.Run();
