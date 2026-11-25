using dymaptic.GeoBlazor;
using dymaptic.GeoBlazor.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Sample.WebApp;
using dymaptic.GeoBlazor.Sample.WebApp.Client;
using dymaptic.GeoBlazor.Sample.WebApp.Components;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddProxyHttpClient();

builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddScoped<LayoutService>();
builder.Services.AddSingleton<ISampleSourceProvider, SampleSourceProvider>();
builder.Configuration.AddInMemoryCollection();
builder.Services.AddOutputCache();
builder.Services.AddMemoryCache();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseOutputCache();

app.UseStaticFiles();
app.MapStaticAssets();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MainLayout).Assembly, typeof(Routes).Assembly);

app.MapProxies();
app.Run();
