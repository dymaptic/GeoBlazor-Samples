using FleetDashboard.Shared;
using FleetDashboard.Shared.Routing;


namespace FleetDashboard.Client.Streaming;


public sealed class DeterministicFleetFrameSource(int seed = 1190232) : IFleetFrameSource
{
    public void Reset()
    {
        _startedAt = null;
        _sequence = 0;
        _runId = Guid.NewGuid();
    }

    public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _startedAt ??= observedAtUtc;
        _sequence++;
        double elapsed = Math.Max(0, (observedAtUtc - _startedAt.Value).TotalSeconds);
        VehicleReport[] vehicles = Enumerable.Range(1, 50)
            .Select(id => CreateReport(id, observedAtUtc, elapsed)).ToArray();

        return ValueTask.FromResult(new FleetFrame(_runId, _sequence, observedAtUtc, Array.AsReadOnly(vehicles)));
    }

    private VehicleReport CreateReport(int id, DateTimeOffset instant, double elapsed)
    {
        FleetRoute route = FleetRoutes.All[(id + 1) % FleetRoutes.All.Count];
        double speed = id % 9 == 0 ? 0 : 35 + (id % 11);
        double phase = Math.Abs(((long)seed - 1190232 + ((id - 7) * 137)) % 1000) / 1000.0;
        RoutePoint point = route.AtDistance((phase * route.LengthKm) + (elapsed * speed / 3600));

        return new VehicleReport(id, _sequence, instant, point.Longitude, point.Latitude, speed, route.Id);
    }

    private Guid _runId = Guid.NewGuid();
    private DateTimeOffset? _startedAt;
    private long _sequence;
}
