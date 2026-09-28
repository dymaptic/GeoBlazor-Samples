using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;


namespace FleetDashboard.Client.State;


public sealed class FleetDashboardState
{
    public void Accept(FleetFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        lock (_sync)
        {
            if (_frame is not null && frame.RunId == _frame.RunId && frame.FrameSequence <= _frame.FrameSequence) return;

            HashSet<int> roster = frame.Vehicles.Select(v => v.VehicleId).ToHashSet();
            if (roster.Count != frame.Vehicles.Count)
            {
                throw new ArgumentException("A fleet frame cannot contain duplicate vehicle identifiers.", nameof(frame));
            }

            if (_frame?.RunId != frame.RunId)
            {
                ResetForNewRun(roster);
            }

            RemoveVehiclesOutsideRoster(roster);
            foreach (VehicleReport report in frame.Vehicles)
            {
                if (_reports.TryGetValue(report.VehicleId, out VehicleReport? current)
                    && report.Sequence <= current.Sequence)
                {
                    continue;
                }

                _reports[report.VehicleId] = report;
                MarkDirty(report);
                AddToHistory(report);
            }

            _frame = frame with { Vehicles = Array.AsReadOnly(frame.Vehicles.ToArray()) };
            if (_selectedId is int selectedId && !_reports.ContainsKey(selectedId)) _selectedId = null;
        }
    }

    public PendingFleetBatch CaptureDirty()
    {
        lock (_sync)
        {
            VehicleReport[] reports = _dirtyReports.Values.OrderBy(report => report.VehicleId).ToArray();
            int[] removedVehicleIds = _removedVehicleIds.Order().ToArray();
            _dirtyReports.Clear();
            _removedVehicleIds.Clear();
            return new PendingFleetBatch(Array.AsReadOnly(reports), Array.AsReadOnly(removedVehicleIds));
        }
    }

    public int DirtyVehicleCount
    {
        get { lock (_sync) return _dirtyReports.Count; }
    }

    public long SupersededPendingReportCount
    {
        get { lock (_sync) return _supersededPendingReportCount; }
    }

    public void Select(int? vehicleId)
    {
        lock (_sync)
        {
            _selectedId = vehicleId is int id && _reports.ContainsKey(id) ? id : null;
        }
    }

    public FleetDashboardView CreateView(DateTimeOffset instant)
    {
        lock (_sync)
        {
            VehicleListItem[] vehicles = _reports.Values.OrderBy(v => v.VehicleId)
                .Select(v => VehicleListItems.Create(v, instant))
                .ToArray();

            // Staleness changes with time, not with reports, so a status change also queues the
            // vehicle for the map. Otherwise a silent truck keeps its last map symbol.
            foreach (VehicleListItem vehicle in vehicles)
            {
                if (_mapStatuses.TryGetValue(vehicle.VehicleId, out VehicleStatus status)
                    && status == vehicle.Status) continue;
                _mapStatuses[vehicle.VehicleId] = vehicle.Status;
                _dirtyReports.TryAdd(vehicle.VehicleId, _reports[vehicle.VehicleId]);
            }

            return new FleetDashboardView(
                Array.AsReadOnly(vehicles),
                vehicles.FirstOrDefault(v => v.VehicleId == _selectedId),
                CreateSelectedHistory(instant),
                _frame?.PublishedAtUtc,
                _frame is not null);
        }
    }

    private IReadOnlyList<SpeedSample> CreateSelectedHistory(DateTimeOffset instant)
    {
        if (_selectedId is not int vehicleId
            || !_history.TryGetValue(vehicleId, out Queue<SpeedSample>? samples))
        {
            return [];
        }

        return Array.AsReadOnly(samples
            .Where(sample => sample.ObservedAtUtc >= instant.AddSeconds(-60))
            .ToArray());
    }

    private void ResetForNewRun(IReadOnlySet<int> roster)
    {
        foreach (int vehicleId in _reports.Keys.Where(vehicleId => !roster.Contains(vehicleId)).ToArray())
        {
            _removedVehicleIds.Add(vehicleId);
        }

        _reports.Clear();
        _dirtyReports.Clear();
        _mapStatuses.Clear();
        _history.Clear();
        _supersededPendingReportCount = 0;
    }

    private void RemoveVehiclesOutsideRoster(IReadOnlySet<int> roster)
    {
        foreach (int vehicleId in _reports.Keys.Where(vehicleId => !roster.Contains(vehicleId)).ToArray())
        {
            _reports.Remove(vehicleId);
            _dirtyReports.Remove(vehicleId);
            _mapStatuses.Remove(vehicleId);
            _history.Remove(vehicleId);
            _removedVehicleIds.Add(vehicleId);
            if (_selectedId == vehicleId) _selectedId = null;
        }
    }

    private void MarkDirty(VehicleReport report)
    {
        if (_dirtyReports.ContainsKey(report.VehicleId)) _supersededPendingReportCount++;
        _dirtyReports[report.VehicleId] = report;
    }

    private void AddToHistory(VehicleReport report)
    {
        if (!_history.TryGetValue(report.VehicleId, out Queue<SpeedSample>? samples))
        {
            samples = new Queue<SpeedSample>();
            _history.Add(report.VehicleId, samples);
        }

        if (samples.Count > 0)
        {
            DateTimeOffset last = samples.Last().ObservedAtUtc;
            if (report.ObservedAtUtc <= last) return;
            // The chart is labeled "last 60 seconds": sample at most once per second per vehicle,
            // however fast the pipeline accepts reports. The 100 ms slack absorbs timer jitter.
            if (report.ObservedAtUtc - last < TimeSpan.FromSeconds(0.9)) return;
        }

        samples.Enqueue(new SpeedSample(report.ObservedAtUtc, report.SpeedKph));
        if (samples.Count > 60) samples.Dequeue();
    }

    private readonly object _sync = new();
    private readonly Dictionary<int, VehicleReport> _reports = [];
    private readonly Dictionary<int, VehicleReport> _dirtyReports = [];
    private readonly Dictionary<int, VehicleStatus> _mapStatuses = [];
    private readonly HashSet<int> _removedVehicleIds = [];
    private FleetFrame? _frame;
    private int? _selectedId;
    private readonly Dictionary<int, Queue<SpeedSample>> _history = [];
    private long _supersededPendingReportCount;
}
