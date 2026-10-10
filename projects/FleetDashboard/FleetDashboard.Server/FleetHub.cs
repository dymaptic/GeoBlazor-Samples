using FleetDashboard.Server.Simulation;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server;

public sealed class FleetHub(FleetSimulator simulator, FleetFrameBuilder frameBuilder, FleetBroadcaster broadcaster)
    : Hub
{
    public FleetFrame GetSnapshot() => frameBuilder.CurrentFrame;

    public void SetPreset(string presetName)
    {
        ScenarioSettings settings;
        try
        {
            settings = ScenarioSettings.ByName(presetName);
        }
        catch (ArgumentException exception)
        {
            throw new HubException(exception.Message);
        }

        simulator.Configure(settings);
        broadcaster.Configure(settings);
    }

    public void PauseVehicle(int vehicleId) => simulator.PauseVehicle(vehicleId);

    public void ResumeVehicle(int vehicleId) => simulator.ResumeVehicle(vehicleId);

    public void PauseFleet() => simulator.PauseFleet();

    public void ResumeFleet() => simulator.ResumeFleet();

    public void Reset()
    {
        // The simulator resets first. The reverse order lets a new-run frame carry the old run's
        // high sequence numbers, and the client would then discard the restarted trucks'
        // sequence-1 reports as older and freeze them. In this order, a frame published between
        // the two calls carries the old run ID with new low sequences, which the client ignores
        // for at most one frame.
        simulator.Reset();
        frameBuilder.StartNewRun();
    }

    /// <summary>Aborts this client's transport to rehearse a real connection break. Unlike a
    /// client-side StopAsync, this severs the connection from the server, so the client's
    /// automatic reconnect and snapshot reconciliation are exercised.</summary>
    public void BreakConnection() => Context.Abort();
}
