using Microsoft.Extensions.Configuration;


namespace dymaptic.GeoBlazor.Pro.Sample.Maui;

public partial class App
{
    public App(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeComponent();
    }

    protected override void OnSleep()
    {
        var apiKey = _configuration.GetValue<string?>("ArcGISApiKey", null);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            Preferences.Set("ArcGISApiKey", apiKey);
        }
        base.OnSleep();
    }
    
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "GeoBlazor Samples" };
    }

    private readonly IConfiguration _configuration;
}