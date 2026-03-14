using Uno.Extensions.Maui;

namespace FieldAssetInspector;

public partial class App : EmbeddingApplication
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        MainWindow = new Window();
#if DEBUG
        MainWindow.EnableHotReload();
#endif

        Host = UnoHost
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<HttpClient>();
            })
            .UseMauiEmbedding<MauiControlsApp>()
            .Build();

        MainWindow.Content = new MainPage();
        MainWindow.Activate();
    }
}
