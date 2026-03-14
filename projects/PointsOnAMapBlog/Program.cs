using dymaptic.GeoBlazor.Core;
using Microsoft.AspNetCore.StaticFiles;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddGeoBlazor(builder.Configuration);

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//supporting the .wsv file type.
FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();
provider.Mappings[".wsv"] = "application/octet-stream";

app.UseStaticFiles();

// NOTE: for some reason, you still need the plain "UseStaticFiles" call above
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();