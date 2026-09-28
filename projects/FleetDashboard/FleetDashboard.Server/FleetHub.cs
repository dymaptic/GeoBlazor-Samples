using FleetDashboard.Server.Simulation;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server;

public sealed class FleetHub(FleetSimulator simulator) : Hub
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

    /// <summary>Aborts this client's transport to rehearse a real connection break. Unlike a
    /// client-side StopAsync, this severs the connection from the server, so the client's
    /// automatic reconnect and snapshot reconciliation are exercised.</summary>
    public void BreakConnection() => Context.Abort();
}
