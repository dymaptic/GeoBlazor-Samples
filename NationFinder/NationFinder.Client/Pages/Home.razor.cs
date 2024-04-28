using Blazored.LocalStorage;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Components.Layers;
using dymaptic.GeoBlazor.Core.Components.Symbols;
using dymaptic.GeoBlazor.Core.Components.Views;
using dymaptic.GeoBlazor.Core.Events;
using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Core.Objects;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace NationFinder.Client.Pages;

public partial class Home : IAsyncDisposable
{
    [Inject]
    public required SignalRClient SignalRClient { get; set; }

    [Inject]
    public required ILocalStorageService LocalStorageService { get; set; }

    [Inject]
    public required GeometryEngine GeometryEngine { get; set; }

    private Graphic? WorldPolygonGraphic => _worldPolygon is not null
        ? new Graphic(_worldPolygon, new SimpleFillSymbol(color: new MapColor("black")))
        : null;

    public async ValueTask DisposeAsync()
    {
        if (_sceneView != null) await _sceneView.DisposeAsync();
        await SignalRClient.DisposeAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await SignalRClient.InitializeAsync();
        SignalRClient.SetSelectedCountry = SetSelectedCountry;
        SignalRClient.ResetGameState = ResetGame;
        SignalRClient.GameOverNotice = GameOver;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_username is null && await LocalStorageService.ContainKeyAsync("username"))
        {
            _username = await LocalStorageService.GetItemAsync<string>("username");
            _isRegistered = true;
            await SignalRClient.RegisterUser(_username!, _email);
            StateHasChanged();
        }
    }
    
    private async Task GameOver(string country)
    {
        _gameOver = true;
        _correctCountry = country;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnViewRendered()
    {
        _correctCountry ??= await SignalRClient.GetSelectedCountry();
    }

    private void SetSelectedCountry(string country)
    {
        _correctCountry = country;
    }

    private async Task OnLayerViewCreated(LayerViewCreateEvent createEvent)
    {
        if (createEvent.Layer?.Id == _worldImageryBasemap?.Id)
        {
            foreach (Sublayer sublayer in _worldImageryBasemap!.AllSublayers!) await sublayer.SetPopupEnabled(false);

            _worldPolygon ??= await GeometryEngine.PolygonFromExtent(_worldImageryBasemap!.FullExtent!);
        }
    }

    private async Task Register()
    {
        try
        {
            CommunicationResult result = await SignalRClient.RegisterUser(_username!, _email);

            if (result.Success)
            {
                _isRegistered = true;
                await LocalStorageService.SetItemAsync("username", _username);
            }
            else
            {
                _error = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _error = ex.ToString();
        }
    }

    private async Task ResetGame()
    {
        _guessSubmitted = false;
        _gameOver = false;
        _gameStarted = false;
        _ok = false;
        _selectedCountry = null;
        _correctCountry = null;
        await _selectedGraphicsLayer!.Clear();
        await _sketchLayer!.Clear();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnMapClicked(ClickEvent clickEvent)
    {
        _cursor = "wait";

        if (_guessSubmitted) return;

        Query query = new() { Geometry = clickEvent.MapPoint, ReturnGeometry = true, OutFields = ["*"] };

        FeatureSet? result = await _countriesLayer!.QueryFeatures(query);

        Graphic? selectedCountryGraphic = result?.Features?.FirstOrDefault();

        if (selectedCountryGraphic is not null)
        {
            _selectedCountry = selectedCountryGraphic.Attributes["COUNTRY"]?.ToString();
            await _selectedGraphicsLayer!.Clear();

            Symbol fillSymbol = new SimpleFillSymbol(new Outline(new MapColor("lightblue"), 4),
                new MapColor(251, 205, 128));

            var clone = new Graphic(selectedCountryGraphic.Geometry, fillSymbol,
                attributes: selectedCountryGraphic.Attributes);
            await _selectedGraphicsLayer.Add(WorldPolygonGraphic!);
            await _selectedGraphicsLayer!.Add(clone);
            await _sceneView!.GoTo(selectedCountryGraphic.Geometry!.Extent!);
        }
        else
        {
            await _selectedGraphicsLayer!.Clear();
            await _sceneView!.ClearGraphics();
        }

        _cursor = "default";
    }

    private async Task ClearSelection()
    {
        _selectedCountry = null;
        await _selectedGraphicsLayer!.Clear();
        await _sceneView!.ClearGraphics();
        await _sceneView!.GoTo(_worldImageryBasemap!.FullExtent!);
    }

    private async Task SubmitGuess()
    {
        await SignalRClient.SubmitGuess(_selectedCountry!, _username!);
        await _selectedGraphicsLayer!.Clear();
        await _sceneView!.GoTo(_worldImageryBasemap!.FullExtent!);
        _guessSubmitted = true;
    }

    private async Task Explore()
    {
        _ok = true;
        await InvokeAsync(StateHasChanged);
    }

    private static readonly IComponentRenderMode InteractiveAuto = new InteractiveAutoRenderMode(false);
    private string? _correctCountry;
    private FeatureLayer? _countriesLayer;
    private string _cursor = "default";
    private string? _email;
    private string? _error;
    private bool _gameOver;
    private bool _gameStarted;
    private bool _guessSubmitted;
    private bool _isRegistered;
    private bool _ok;
    private SceneView? _sceneView;
    private string? _selectedCountry;
    private GraphicsLayer? _selectedGraphicsLayer;
    private GraphicsLayer? _sketchLayer;
    private string? _username;
    private TileLayer? _worldImageryBasemap;
    private Polygon? _worldPolygon;
}