using Blazored.LocalStorage;
using dymaptic.GeoBlazor.Core.Components.Views;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace NationFinder.Client.Pages;

public partial class Home: IAsyncDisposable
{
    [Inject] 
    public required SignalRClient SignalRClient { get; set; }

    [Inject] 
    public required ILocalStorageService LocalStorageService { get; set; }
    
    public async ValueTask DisposeAsync()
    {
        if (_mapView != null) await _mapView.DisposeAsync();
        if (_sceneView != null) await _sceneView.DisposeAsync();
        await SignalRClient.DisposeAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await SignalRClient.InitializeAsync();
        SignalRClient.UpdateState = UpdateState;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_username is null && await LocalStorageService.ContainKeyAsync("username"))
        {
            _username = await LocalStorageService.GetItemAsync<string>("username");
            StateHasChanged();
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

    private void UpdateState(LayerState layerState)
    {
        _showScene = layerState.ShowScene;
        _showNight = layerState.ShowNight;
        _showBorders = layerState.ShowBorders;
        _showWeather = layerState.ShowWeather;
        
        StateHasChanged();
        if (_showScene && _sceneView is not null)
        {
            _sceneView.Refresh();
        }
        else if (!_showScene && _mapView is not null)
        {
            _mapView.Refresh();
        }
    }

    private bool _isRegistered;
    private string? _username;
    private string? _error;
    private bool _showScene;
    private bool _showWeather;
    private bool _showTopography;
    private bool _showBorders;
    private bool _showNight;
    private bool _showTemp;
    private MapView? _mapView;
    private SceneView? _sceneView;
    private static IComponentRenderMode? _interactiveWasm = new InteractiveWebAssemblyRenderMode(false);
}