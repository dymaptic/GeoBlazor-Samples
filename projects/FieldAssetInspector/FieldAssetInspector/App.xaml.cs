using System.Reflection;
using dymaptic.GeoBlazor.Core;
using FieldAssetInspector.Razor;
using Microsoft.Extensions.Configuration;
using Application = Microsoft.Maui.Controls.Application;

namespace FieldAssetInspector;

public partial class App : Microsoft.UI.Xaml.Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Build IConfiguration from embedded appsettings.json (one read, use everywhere)
        // or User Secrets during development
        Assembly assembly = typeof(App).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(typeof(App),
            "appsettings.json")!;

        IConfiguration appConfig = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .AddUserSecrets<App>()
            .Build();

        IApplicationBuilder appBuilder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host.ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddConfiguration(appConfig);
                });
                host.ConfigureServices((context, services) => { services.AddSingleton<HttpClient>(); });
            })
            .UseMauiEmbedding<Application>(maui =>
            {
                maui.Services.AddSingleton<IConfiguration>(appConfig);
                maui.Services.AddMauiBlazorWebView();
                maui.Services.AddGeoBlazor(appConfig);
                maui.Services.AddSingleton<AssetSelectionService>();
#if DEBUG
                maui.Services.AddBlazorWebViewDeveloperTools();
#endif
            });

        var host = appBuilder.Build();

        var selection = host.Services.GetRequiredService<AssetSelectionService>();
        appBuilder.Window.Content = new MainPage(selection);
        appBuilder.Window.Activate();
    }
}
