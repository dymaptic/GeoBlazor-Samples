namespace FleetDashboard.Client.Streaming;

public interface IFleetScenarioControls
{
    Task PauseVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task ResumeVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);

    Task PauseFleetAsync(CancellationToken cancellationToken = default);

    Task ResumeFleetAsync(CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);

    Task SetPresetAsync(string presetName, CancellationToken cancellationToken = default);

    /// <summary>Asks the server to abort this connection so automatic reconnect can be rehearsed.</summary>
    Task BreakConnectionAsync(CancellationToken cancellationToken = default);
}
