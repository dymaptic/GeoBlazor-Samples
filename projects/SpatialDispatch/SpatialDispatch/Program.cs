using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO.Converters;
using SpatialDispatch.Api;
using SpatialDispatch.Components;
using SpatialDispatch.Data;
using SpatialDispatch.Shared.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// GeoBlazor is registered in the client alone. Prerendering and server interactivity are both off, so no
// GeoBlazor component ever renders in this process, and the license key lives in the client's
// wwwroot/appsettings.Development.json where only the WebAssembly configuration can read it. Turning either
// back on means registering GeoBlazor here too, reading the key from that same file: a host that guessed at
// the key would register Core while the client registered Pro, and RouteService would not resolve.

string connectionString = builder.Configuration.GetConnectionString("DispatchDatabase")
    ?? throw new InvalidOperationException(
        "No DispatchDatabase connection string. Copy appsettings.Development.json.template to "
        + "appsettings.Development.json in this project and set the server there, then start the "
        + "database with `docker compose up -d`. The README has both steps.");

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
