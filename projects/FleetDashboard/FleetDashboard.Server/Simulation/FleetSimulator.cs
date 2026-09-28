using FleetDashboard.Shared;
using FleetDashboard.Shared.Routing;

namespace FleetDashboard.Server.Simulation;

public sealed class FleetSimulator
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _startedAtUtc;
    private readonly Dictionary<int, long> _vehicleSequences = [];
    private readonly Dictionary<int, VehicleReport> _heldReports = [];
    private readonly Dictionary<int, double> _pauseStartElapsed = [];
    private readonly Dictionary<int, double> _skippedSeconds = [];
    private Guid _runId = Guid.NewGuid();
    private FleetFrame _currentFrame;
    private long _frameSequence;
    private double _simulatedElapsedSeconds;
    private DateTimeOffset _lastAdvancedAtUtc;
    private bool _fleetPaused;

    public FleetSimulator(TimeProvider timeProvider, ScenarioSettings settings)
    {
        _timeProvider = timeProvider;
        Settings = settings;
        _startedAtUtc = timeProvider.GetUtcNow();
        _lastAdvancedAtUtc = _startedAtUtc;
        _currentFrame = CreateFrame(_startedAtUtc);
    }

    public ScenarioSettings Settings { get; private set; }

    /// <summary>Reconfigures the workload preset. The roster changes on the next frame; existing
    /// vehicles keep their sequences and vehicles above the new roster size simply disappear.</summary>
    public void Configure(ScenarioSettings settings)
    {
        lock (_gate)
        {
            Settings = settings;
            foreach (int vehicleId in _heldReports.Keys.Where(id => id > settings.VehicleCount).ToArray())
            {
                _heldReports.Remove(vehicleId);
                _pauseStartElapsed.Remove(vehicleId);
                _skippedSeconds.Remove(vehicleId);
            }
        }
    }

    public FleetFrame CurrentFrame => Volatile.Read(ref _currentFrame);

    public FleetFrame Advance()
    {
        lock (_gate)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            FleetFrame frame = CreateFrame(now);
            Volatile.Write(ref _currentFrame, frame);
            return frame;
        }
    }

    public void PauseVehicle(int vehicleId)
    {
        ValidateVehicleId(vehicleId);
        lock (_gate)
        {
            if (_heldReports.ContainsKey(vehicleId)) return;
            _heldReports[vehicleId] = _currentFrame.Vehicles.First(v => v.VehicleId == vehicleId);
            _pauseStartElapsed[vehicleId] = _simulatedElapsedSeconds;
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

            _heldReports.Remove(vehicleId);
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

    public void Reset()
    {
        lock (_gate)
        {
            _runId = Guid.NewGuid();
            _frameSequence = 0;
            _vehicleSequences.Clear();
            _heldReports.Clear();
            _pauseStartElapsed.Clear();
            _skippedSeconds.Clear();
            _fleetPaused = false;
            _simulatedElapsedSeconds = 0;
            _lastAdvancedAtUtc = _timeProvider.GetUtcNow();
            Volatile.Write(ref _currentFrame, CreateFrame(_lastAdvancedAtUtc));
        }
    }

    private FleetFrame CreateFrame(DateTimeOffset publishedAtUtc)
    {
        long frameSequence = ++_frameSequence;
        if (!_fleetPaused)
        {
            _simulatedElapsedSeconds += Math.Max(0, (publishedAtUtc - _lastAdvancedAtUtc).TotalSeconds);
        }

        _lastAdvancedAtUtc = publishedAtUtc;
        if (_fleetPaused)
        {
            return new FleetFrame(_runId, frameSequence, publishedAtUtc, _currentFrame.Vehicles);
        }

        double elapsedSeconds = _simulatedElapsedSeconds;
        VehicleReport[] reports = new VehicleReport[Settings.VehicleCount];

        for (int vehicleId = 1; vehicleId <= Settings.VehicleCount; vehicleId++)
        {
            if (_heldReports.TryGetValue(vehicleId, out VehicleReport? held))
            {
                reports[vehicleId - 1] = held;
                continue;
            }

            FleetRoute route = FleetRoutes.All[(vehicleId + 1) % FleetRoutes.All.Count];
            double speedKph = vehicleId % 9 == 0 ? 0 : 35 + (vehicleId % 11);
            double phase = ((vehicleId * 137) % 1000) / 1000d;
            double skipped = _skippedSeconds.TryGetValue(vehicleId, out double value) ? value : 0;
            double effectiveElapsed = elapsedSeconds - skipped;
            RoutePoint point = route.AtDistance((phase * route.LengthKm) + (effectiveElapsed * speedKph / 3600d));
            long sequence = _vehicleSequences.TryGetValue(vehicleId, out long current) ? current + 1 : 1;
            _vehicleSequences[vehicleId] = sequence;
            reports[vehicleId - 1] = new VehicleReport(vehicleId, sequence, publishedAtUtc,
                point.Longitude, point.Latitude, speedKph, route.Id);
        }

        return new FleetFrame(_runId, frameSequence, publishedAtUtc, Array.AsReadOnly(reports));
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
