using dymaptic.GeoBlazor.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace FieldAssetInspector.MauiControls;

/// <summary>
/// Configures MAUI services for the embedded Blazor components,
/// including GeoBlazor Pro registration and configuration.
/// </summary>
public class MauiControlsApp : Microsoft.Maui.Controls.Application
{
    /// <summary>
    /// Registers MAUI services including BlazorWebView and GeoBlazor Pro.
    /// Called during MAUI Embedding initialization.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMauiBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        services.AddGeoBlazor(configuration);
    }
}
