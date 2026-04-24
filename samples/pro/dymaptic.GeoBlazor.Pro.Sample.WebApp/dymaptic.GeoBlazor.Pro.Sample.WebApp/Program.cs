using dymaptic.GeoBlazor.Core.Sample.Shared.Pages;
using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Pro;
using dymaptic.GeoBlazor.Pro.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Pro.Sample.WebApp;
using dymaptic.GeoBlazor.Pro.Sample.WebApp.Client;
using dymaptic.GeoBlazor.Pro.Sample.WebApp.Components;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddProxyHttpClient();

builder.Services.AddGeoBlazorPro(builder.Configuration);
builder.Services.AddScoped<LayoutService>();
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
    .AddAdditionalAssemblies(typeof(Navigation).Assembly, 
        typeof(ProMainLayout).Assembly,
        typeof(Routes).Assembly);

app.MapProxies();
app.Run();