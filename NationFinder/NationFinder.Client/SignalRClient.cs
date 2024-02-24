using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace NationFinder.Client;

public class SignalRClient(NavigationManager navigation): IAsyncDisposable, ISignalRClient
{
    public Action<LayerState>? UpdateState;

    public async Task InitializeAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        CancellationToken token = _cts.Token;
        
        while (true)
        {
            try
            {
                Console.WriteLine("Initializing SignalR Connection...");
                await _hubConnection.StartAsync(token);
                Debug.Assert(_hubConnection.State == HubConnectionState.Connected);
                Console.WriteLine("Connected to SignalR Hub.");
                break;
            }
            catch when (token.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                Console.WriteLine("Connection failed, retrying in 1 sec...");
                Debug.Assert(_hubConnection.State == HubConnectionState.Disconnected);
                await Task.Delay(1000, token);
            }
        }
        
        _hubConnection.Closed += error =>
        {
            Console.WriteLine($"Connection closed. {error?.Message}");
            Debug.Assert(_hubConnection.State == HubConnectionState.Disconnected);
            Task.Delay(1000, token);
            return InitializeAsync();
        };

        _hubConnection.On<LayerState>(nameof(ToggleState), ToggleState);
    }

    public async Task<CommunicationResult> RegisterUser(string username)
    {
        return await _hubConnection.InvokeAsync<CommunicationResult>(nameof(RegisterUser), username, _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _hubConnection.DisposeAsync();
    }

    public void ToggleState(LayerState state)
    {
        UpdateState!(state);
    }

    private readonly HubConnection _hubConnection = new HubConnectionBuilder()
        .WithUrl(navigation.ToAbsoluteUri(ConnectUrl))
        .WithAutomaticReconnect()
        .Build();
    public static readonly string ConnectUrl = "/connect";
    private CancellationTokenSource _cts = new();
}

public interface ISignalRClient
{
    public Task<CommunicationResult> RegisterUser(string username);
}