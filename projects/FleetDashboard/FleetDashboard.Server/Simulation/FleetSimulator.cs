using FleetDashboard.Server.Telemetry;
using FleetDashboard.Shared;
using FleetDashboard.Shared.Routing;

namespace FleetDashboard.Server.Simulation;

/// <summary>
/// Stands in for the fleet's GPS devices and gateway. On its own clock, every truck that is not
/// paused writes a new report into the <see cref="VehicleReportStore"/>. It never builds frames:
/// publishing what the store holds is the server's job, on a separate clock.
/// </summary>
public sealed class FleetSimulator : BackgroundService
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly VehicleReportStore _store;
    private readonly Dictionary<int, long> _vehicleSequences = [];
    // Paused vehicles, each mapped to the simulated elapsed time at which it paused.
    private readonly Dictionary<int, double> _pauseStartElapsed = [];
    private readonly Dictionary<int, double> _skippedSeconds = [];
    private double _simulatedElapsedSeconds;
    private DateTimeOffset _lastAdvancedAtUtc;
    private bool _fleetPaused;

    public FleetSimulator(TimeProvider timeProvider, ScenarioSettings settings, VehicleReportStore store)
    {
        _timeProvider = timeProvider;
        _store = store;
        Settings = settings;
        _lastAdvancedAtUtc = timeProvider.GetUtcNow();

        // Populate the store before the first frame is built.
        RecordReports(_lastAdvancedAtUtc);
    }

    public ScenarioSettings Settings { get; private set; }

    /// <summary>Reconfigures the workload preset. New vehicles report on the next tick; existing
    /// vehicles keep their sequences, and vehicles above the new roster size leave the store, so
    /// they disappear from the next frame.</summary>
    public void Configure(ScenarioSettings settings)
    {
        lock (_gate)
        {
            for (int vehicleId = settings.VehicleCount + 1; vehicleId <= Settings.VehicleCount; vehicleId++)
            {
                _store.Remove(vehicleId);
                _pauseStartElapsed.Remove(vehicleId);
                _skippedSeconds.Remove(vehicleId);
            }

            Settings = settings;
        }
    }

    /// <summary>Moves simulated time to now and records a new report for every vehicle that is not
    /// paused. While the whole fleet is paused nothing is written, so the store keeps every last
    /// report and the dashboard sees the fleet go stale.</summary>
    public void Advance()
    {
        lock (_gate)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            TimeSpan sinceLastAdvance = now - _lastAdvancedAtUtc;
            _lastAdvancedAtUtc = now;

            // Simulated time stands still while the fleet is paused, so trucks resume where they stopped.
            if (_fleetPaused) return;

            _simulatedElapsedSeconds += Math.Max(0, sinceLastAdvance.TotalSeconds);
            RecordReports(now);
        }
    }

    public void PauseVehicle(int vehicleId)
    {
        ValidateVehicleId(vehicleId);
        lock (_gate)
        {
            _pauseStartElapsed.TryAdd(vehicleId, _simulatedElapsedSeconds);
        }
    }

    public void ResumeVehicle(int vehicleId)
    {
        ValidateVehicleId(vehicleId);
        lock (_gate)
        {
            if (_pauseStartElapsed.TryGetValue(vehicleId, out double startedAt))
            {
                double pausedFor = Math.Max(0, _simulatedElapsedSeconds - startedAt);
                _skippedSeconds[vehicleId] = (_skippedSeconds.TryGetValue(vehicleId, out double prior) ? prior : 0)
                    + pausedFor;
                _pauseStartElapsed.Remove(vehicleId);
            }
        }
    }

    public void PauseFleet()
    {
        lock (_gate)
        {
            _fleetPaused = true;
        }
    }

    public void ResumeFleet()
    {
        lock (_gate)
        {
            _fleetPaused = false;
        }
    }

    /// <summary>Restarts every vehicle at its starting position and sequence 1. The store is
    /// overwritten rather than cleared: replacing every vehicle guarantees it never holds an old
    /// run's sequence after the reset, and never goes empty, which would publish an empty frame
    /// and blink the map.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _vehicleSequences.Clear();
            _pauseStartElapsed.Clear();
            _skippedSeconds.Clear();
            _fleetPaused = false;
            _simulatedElapsedSeconds = 0;
            _lastAdvancedAtUtc = _timeProvider.GetUtcNow();
            RecordReports(_lastAdvancedAtUtc);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Advance();

                // Re-read the report rate each tick so a preset change applies without a restart.
                await Task.Delay(TimeSpan.FromSeconds(1d / Settings.ReportsPerSecond), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>Writes a report for every roster vehicle that is not paused. Callers hold the lock.</summary>
    private void RecordReports(DateTimeOffset observedAtUtc)
    {
        for (int vehicleId = 1; vehicleId <= Settings.VehicleCount; vehicleId++)
        {
            // Skip before taking a sequence number, so a resumed vehicle continues at sequence + 1.
            if (_pauseStartElapsed.ContainsKey(vehicleId)) continue;

            _store.Record(CreateReport(vehicleId, observedAtUtc));
        }
    }

    private VehicleReport CreateReport(int vehicleId, DateTimeOffset observedAtUtc)
    {
        FleetRoute route = FleetRoutes.All[(vehicleId + 1) % FleetRoutes.All.Count];
        double speedKph = vehicleId % 9 == 0 ? 0 : 35 + (vehicleId % 11);
        double phase = ((vehicleId * 137) % 1000) / 1000d;
        double skipped = _skippedSeconds.TryGetValue(vehicleId, out double value) ? value : 0;
        double effectiveElapsed = _simulatedElapsedSeconds - skipped;
        RoutePoint point = route.AtDistance((phase * route.LengthKm) + (effectiveElapsed * speedKph / 3600d));
        long sequence = _vehicleSequences.TryGetValue(vehicleId, out long current) ? current + 1 : 1;
        _vehicleSequences[vehicleId] = sequence;
        return new VehicleReport(vehicleId, sequence, observedAtUtc,
            point.Longitude, point.Latitude, speedKph, route.Id);
    }

    private void ValidateVehicleId(int vehicleId)
    {
        if (vehicleId < 1 || vehicleId > Settings.VehicleCount)
        {
            throw new ArgumentOutOfRangeException(nameof(vehicleId), vehicleId,
                $"The fleet roster contains vehicles 1 through {Settings.VehicleCount}.");
        }
    }
}
