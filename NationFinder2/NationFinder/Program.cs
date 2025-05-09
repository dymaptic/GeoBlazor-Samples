using Blazored.LocalStorage;
using dymaptic.GeoBlazor.Core;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using NationFinder;
using NationFinder.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddGeoBlazor(builder.Configuration);
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler("/Error", createScopeForErrors: true);
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();


app.UseHttpsRedirection();
app.UseResponseCompression();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".wsv"] = "application/octet-stream";
provider.Mappings.Remove(".map");

app.UseStaticFiles();
// NOTE: for some reason, you still need the plain "UseStaticFiles" call above
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
