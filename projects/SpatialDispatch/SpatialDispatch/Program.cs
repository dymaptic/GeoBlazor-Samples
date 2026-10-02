using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO.Converters;
using SpatialDispatch.Api;
using SpatialDispatch.Components;
using SpatialDispatch.Data;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// GeoBlazor is registered in the client alone, but prerendering is on, so the dependencies the prerender
// pass reaches have to exist here as well: the board reads its data through SqlServerDispatchService and
// its route through the straight-line fallback registered below. The map is the one component that cannot
// render on the server, so DispatchBoard holds it back until the browser has taken over. That is what keeps
// GeoBlazor, and the license key in the client's wwwroot/appsettings.Development.json, out of this process.

string connectionString = builder.Configuration.GetConnectionString("DispatchDatabase")
    ?? throw new InvalidOperationException(
        "No DispatchDatabase connection string. In Development, copy appsettings.Development.json.template "
        + "to appsettings.Development.json and set the server there; outside Development, set the "
        + "ConnectionStrings__DispatchDatabase environment variable. Then start the database with "
        + "`docker compose up -d`. The README has both steps.");

builder.Services.AddDbContext<DispatchDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServer =>
    {
        // The one line that turns EF Core's SQL Server provider spatial. Without it, a NetTopologySuite
        // property is just an unmappable CLR type.
        sqlServer.UseNetTopologySuite();

        // Covers faults on a server that is already answering. It does not cover a container that has not
        // finished starting: see DatabaseAvailability, which is what handles that case.
        sqlServer.EnableRetryOnFailure(
            maxRetryCount: 12,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddScoped<IDispatchService, SqlServerDispatchService>();

// Prerendering instantiates the board on the server, so every service the component tree injects has to
// resolve here as well as in the client. Routing is the one that differs: the server has no ArcGIS route
// service to call, so it registers the same straight-line fallback the client uses when a key is missing.
// That registration exists only to satisfy DI for the prerender pass; IRouteService is invoked when a job
// is selected, which cannot happen until the WebAssembly app takes over.
builder.Services.AddScoped<IRouteService, StraightLineRouteService>();

// Teaches System.Text.Json to write NetTopologySuite geometries as GeoJSON. ConfigureHttpJsonOptions is the
// Minimal API entry point; AddControllers().AddJsonOptions(), which the package README shows, configures
// MVC's options and would leave these endpoints emitting the raw NetTopologySuite object graph.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new GeoJsonConverterFactory()));

WebApplication app = builder.Build();

// Schema and seed data are applied automatically, so a cold clone needs no manual database preparation.
// Seeding is skipped when the tables already hold the same number of records as the demo contract.
await DatabaseAvailability.WaitForServerAsync(connectionString, TimeSpan.FromMinutes(2), app.Logger);

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    DispatchDbContext db = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
    await db.Database.MigrateAsync();
    await DemoDataSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapDispatchEndpoints();
app.MapMapLayerEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SpatialDispatch.Client._Imports).Assembly);

app.Run();
