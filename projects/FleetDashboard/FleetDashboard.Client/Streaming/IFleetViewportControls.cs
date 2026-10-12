using FleetDashboard.Shared;

namespace FleetDashboard.Client.Streaming;

/// <summary>Viewport-mode commands plus the per-frame count that shows their effect.</summary>
public interface IFleetViewportControls
{
    /// <summary>Vehicles in the most recent frame received from the server, before merging.</summary>
    int LastReceivedVehicleCount { get; }

    /// <summary>Opts this connection into viewport filtering with the extent the map shows.</summary>
    Task SetViewportAsync(ViewportExtent extent, CancellationToken cancellationToken = default);

    /// <summary>Asks the server to keep this vehicle in every frame; null clears the selection.</summary>
    Task SetSelectedVehicleAsync(int? vehicleId, CancellationToken cancellationToken = default);
}
