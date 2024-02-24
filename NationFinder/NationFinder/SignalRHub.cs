using Microsoft.AspNetCore.SignalR;
using NationFinder.Client;

namespace NationFinder;

public class SignalRHub: Hub, ISignalRClient
{
    public async Task<CommunicationResult> RegisterUser(string username)
    {
        try
        {
            // check if username is already in use
            if (ConnectedUsers.Contains(username))
            {
                return new CommunicationResult(false, "Username already in use");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, "players");
            ConnectedUsers.Add(username);

            return new CommunicationResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new CommunicationResult(false, "Error on Server");
        }
    }

    private static List<string> ConnectedUsers = [];
}