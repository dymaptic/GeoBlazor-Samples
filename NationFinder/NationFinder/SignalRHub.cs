using Microsoft.AspNetCore.SignalR;
using NationFinder.Client;

namespace NationFinder;

public class SignalRHub: Hub, ISignalRClient
{
    public async Task<CommunicationResult> RegisterUser(string username, string? email)
    {
        try
        {
            // check if username is already in use
            if (State.ConnectedUsers.Contains(username))
            {
                return new CommunicationResult(false, "Username already in use");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                State.Emails.Add(email);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, "players");
            State.ConnectedUsers.Add(username);

            return new CommunicationResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new CommunicationResult(false, "Error on Server");
        }
    }

    public async Task SubmitGuess(string country, string username)
    {
        State.UserGuesses[username] = country;
        Console.WriteLine($"{username} guessed {country}");
        if (State.UpdateGameBoard is not null)
        {
            await State.UpdateGameBoard();
        }
    }

    public async Task<string?> GetSelectedCountry()
    {
        return State.CurrentCountry;
    }
}