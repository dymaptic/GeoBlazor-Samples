using Uno.Extensions.Maui;

namespace FieldAssetInspector;

public partial class App : Microsoft.UI.Xaml.Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var appBuilder = this.CreateBuilder(args)
            .Configure(host => host.ConfigureServices((context, services) =>
            {
                services.AddSingleton<HttpClient>();
            }))
            .UseMauiEmbedding<MauiControlsApp>();

        var host = appBuilder.Build();

        appBuilder.Window.Content = new MainPage();
        appBuilder.Window.Activate();
    }
}
