using FleetDashboard.Server.Simulation;
using FleetDashboard.Shared;
using FleetDashboard.Shared.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetSimulatorHostTests
{
    [TestMethod]
    public async Task TheReportingLoopWritesReportsOnItsOwnClock()
    {
        var store = new VehicleReportStore();
        var simulator = new FleetSimulator(TimeProvider.System, ScenarioSettings.Normal, store);
        using var host = new FleetSimulatorHost(simulator);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await host.StartAsync(CancellationToken.None);
        try
        {
            // Sequence 3 takes two loop iterations, so the loop is ticking rather than running once.
            while (store.Latest().Single(r => r.VehicleId == 1).Sequence < 3)
            {
                await Task.Delay(50, timeout.Token);
            }
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }
}
