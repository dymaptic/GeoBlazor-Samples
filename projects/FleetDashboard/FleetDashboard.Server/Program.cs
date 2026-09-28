using FleetDashboard.Server;
using FleetDashboard.Server.Simulation;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;

// Hosts the WebAssembly dashboard, fleet simulator, and SignalR hub in one process.

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(ScenarioSettings.Normal);
builder.Services.AddSingleton<FleetSimulator>();
builder.Services.AddHostedService<FleetBroadcaster>();

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
