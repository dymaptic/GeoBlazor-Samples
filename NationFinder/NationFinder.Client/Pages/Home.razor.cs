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

public partial class Home: IAsyncDisposable
{
    [Inject] 
    public required SignalRClient SignalRClient { get; set; }

    [Inject] 
    public required ILocalStorageService LocalStorageService { get; set; }
    
    [Inject]
    public required GeometryEngine GeometryEngine { get; set; }
    
    public async ValueTask DisposeAsync()
    {
        if (_sceneView != null) await _sceneView.DisposeAsync();
        await SignalRClient.DisposeAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await SignalRClient.InitializeAsync();
        SignalRClient.ResetGameState = ResetGame;
        SignalRClient.GameOverNotice = GameOver;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_username is null && await LocalStorageService.ContainKeyAsync("username"))
        {
            _username = await LocalStorageService.GetItemAsync<string>("username");
            _isRegistered = true;
            await SignalRClient.RegisterUser(_username!);
            StateHasChanged();
        }
    }

    private async Task OnLayerViewCreated(LayerViewCreateEvent createEvent)
    {
        if (createEvent.Layer?.Id == _worldImageryBasemap?.Id)
        {
            foreach (Sublayer sublayer in _worldImageryBasemap!.AllSublayers!)
            {
                await sublayer.SetPopupEnabled(false);
            }

            _worldPolygon ??= await GeometryEngine.PolygonFromExtent(_worldImageryBasemap!.FullExtent!);
        }
    }

    private async Task Register()
    {
        try
        {
            CommunicationResult result = await SignalRClient.RegisterUser(_username!);
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
        _selectedCountry = null;
        _selectedGraphicsLayer?.Clear();
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task OnMapClicked(ClickEvent clickEvent)
    {
        if (_guessSubmitted) return;
        Query query = new()
        {
            Geometry = clickEvent.MapPoint,
            ReturnGeometry = true,
            OutFields = ["*"]
        };
        
        FeatureSet? result = await _countriesLayer!.QueryFeatures(query);
        
        Graphic? selectedCountryGraphic = result?.Features?.FirstOrDefault();
        if (selectedCountryGraphic is not null)
        {
            _selectedCountry = selectedCountryGraphic.Attributes["COUNTRY"]?.ToString();
            await _selectedGraphicsLayer!.Clear();
            Symbol fillSymbol = new SimpleFillSymbol(new Outline(new MapColor("lightblue"), 4), 
                new MapColor(251, 205, 128));
            Graphic clone = new Graphic(selectedCountryGraphic.Geometry, fillSymbol,
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
    
    private async Task GameOver(string country)
    {
        _gameOver = true;
        _correctCountry = country;
        await InvokeAsync(StateHasChanged);
    }

    private Graphic? WorldPolygonGraphic => _worldPolygon is not null
        ? new Graphic(_worldPolygon, new SimpleFillSymbol(color: new MapColor("white")))
        : null;
    private bool _isRegistered;
    private string? _username;
    private string? _error;
    private SceneView? _sceneView;
    private FeatureLayer? _countriesLayer;
    private TileLayer? _worldImageryBasemap;
    private GraphicsLayer? _selectedGraphicsLayer;
    private static readonly IComponentRenderMode InteractiveWasm = new InteractiveWebAssemblyRenderMode(false);
    private Polygon? _worldPolygon;
    private string? _selectedCountry;
    private bool _guessSubmitted;
    private bool _ok;
    private bool _gameOver;
    private string? _correctCountry;
}