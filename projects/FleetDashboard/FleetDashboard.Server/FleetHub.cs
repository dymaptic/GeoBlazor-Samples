using FleetDashboard.Server.Simulation;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server;

public sealed class FleetHub(FleetSimulator simulator, ViewportSubscriptions viewports) : Hub
{
    public FleetFrame GetSnapshot() => simulator.CurrentFrame;

    public void SetPreset(string presetName)
    {
        try
        {
            simulator.Configure(ScenarioSettings.ByName(presetName));
        }
        catch (ArgumentException exception)
        {
            throw new HubException(exception.Message);
        }
    }

    public void PauseVehicle(int vehicleId) => simulator.PauseVehicle(vehicleId);

    public void ResumeVehicle(int vehicleId) => simulator.ResumeVehicle(vehicleId);

    public void PauseFleet() => simulator.PauseFleet();

    public void ResumeFleet() => simulator.ResumeFleet();

    public void Reset() => simulator.Reset();

    /// <summary>Opts this connection into viewport filtering with the extent the map shows, in
    /// WGS84 degrees. Until a connection calls this, it receives every full frame.</summary>
    public void SetViewport(ViewportExtent extent) => viewports.GetOrAdd(Context.ConnectionId).SetExtent(extent);

    /// <summary>Keeps the selected vehicle in every filtered frame wherever it is; null clears it.</summary>
    public void SetSelectedVehicle(int? vehicleId) =>
        viewports.GetOrAdd(Context.ConnectionId).SetSelectedVehicle(vehicleId);

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        viewports.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Aborts this client's transport to rehearse a real connection break. Unlike a
    /// client-side StopAsync, this severs the connection from the server, so the client's
    /// automatic reconnect and snapshot reconciliation are exercised.</summary>
    public void BreakConnection() => Context.Abort();
}
