using FleetDashboard.Shared.Simulation;

namespace FleetDashboard.Server.Simulation;

/// <summary>Runs the simulator's reporting clock for the server: advances the fleet at the preset's
/// report rate until the host stops. The simulator itself has no loop so the browser-only client can
/// drive it from its own tick.</summary>
public sealed class FleetSimulatorHost(FleetSimulator simulator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                simulator.Advance();

                // Re-read the report rate each tick so a preset change applies without a restart.
                await Task.Delay(TimeSpan.FromSeconds(1d / simulator.Settings.ReportsPerSecond), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}