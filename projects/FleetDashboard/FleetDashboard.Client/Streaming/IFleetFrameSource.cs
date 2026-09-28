using FleetDashboard.Shared;


namespace FleetDashboard.Client.Streaming;


public interface IFleetFrameSource
{
    ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken = default);
}
