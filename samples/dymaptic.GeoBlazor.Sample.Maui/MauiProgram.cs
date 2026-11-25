using Microsoft.Extensions.Configuration;
using System.Reflection;
using dymaptic.GeoBlazor.Sample.Shared.Shared;


namespace dymaptic.GeoBlazor.Sample.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddScoped<HttpClient>();
        builder.Configuration.AddInMemoryCollection();

        var executingAssembly = Assembly.GetExecutingAssembly();

        using Stream stream = executingAssembly
            .GetManifestResourceStream("dymaptic.GeoBlazor.Sample.Maui.appsettings.json")!;

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets(executingAssembly)
            .AddJsonStream(stream)
            .Build();
        builder.Configuration.AddConfiguration(config);
        builder.Services.AddGeoBlazor(builder.Configuration);
        builder.Services.AddScoped<LayoutService>();
        builder.Services.AddSingleton<ISampleSourceProvider, SampleSourceProvider>();

        return builder.Build();
    }
}