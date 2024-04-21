using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace NationFinder.Client;

public class SignalRClient(NavigationManager navigation): IAsyncDisposable, ISignalRClient
{
    public Func<Task>? ResetGameState;
    public Func<string, Task>? GameOverNotice;
    public Func<string, Task>? SetSelectedCountry;

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

        _hubConnection.On<string>(nameof(GameOver), GameOver);
        _hubConnection.On(nameof(ResetGame), ResetGame);
        _hubConnection.On<string>(nameof(SetCountry), SetCountry);
    }

    public async Task<CommunicationResult> RegisterUser(string username, string? email)
    {
        return await _hubConnection.InvokeAsync<CommunicationResult>(nameof(RegisterUser), username, email, _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _hubConnection.DisposeAsync();
    }

    public async Task SubmitGuess(string country, string username)
    {
        await _hubConnection.SendAsync(nameof(SubmitGuess), country, username, _cts.Token);
    }

    public async Task<string?> GetSelectedCountry()
    {
        return await _hubConnection.InvokeAsync<string>(nameof(GetSelectedCountry));
    }

    public async Task SetCountry(string country)
    {
        await SetSelectedCountry!(country);
    }

    public async Task ResetGame()
    {
        await ResetGameState!();
    }

    public async Task GameOver(string country)
    {
        await GameOverNotice!(country);
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
    public Task<CommunicationResult> RegisterUser(string username, string? email);

    public Task SubmitGuess(string country, string username);

    public Task<string?> GetSelectedCountry();
}