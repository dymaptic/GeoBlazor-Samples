using FleetDashboard.Shared;

namespace FleetDashboard.Server.Streaming;

/// <summary>
/// Per-connection filter for viewport mode. Each frame carries the vehicles inside the client's
/// extent plus the selected vehicle; every other vehicle rides along at most once per second, with
/// its real observation time, so the list stays complete and nothing trips the stale threshold.
/// </summary>
/// <remarks>
/// The extent and selection are written by hub calls and read by the broadcaster, so they are
/// swapped as one immutable snapshot. The send history is touched only by the single broadcast
/// loop. Time comes from <see cref="FleetFrame.PublishedAtUtc"/>, which keeps the rule deterministic.
/// </remarks>
public sealed class ViewportFrameFilter
{
    public static readonly TimeSpan OutOfViewInterval = TimeSpan.FromSeconds(1);

    /// <summary>Out-of-view vehicles are spread across this many slots of the interval, so the
    /// per-frame count stays steady instead of spiking once a second with the whole roster.</summary>
    private const int StaggerSlots = 10;

    private sealed record ViewState(ViewportExtent? Extent, int? SelectedVehicleId);

    private readonly object _writeGate = new();
    private ViewState _view = new(null, null);
    private readonly Dictionary<int, DateTimeOffset> _lastSentUtc = [];
    private HashSet<int>? _roster;

    public ViewportExtent? Extent => Volatile.Read(ref _view).Extent;

    public void SetExtent(ViewportExtent extent) => Update(view => view with { Extent = extent });

    public void SetSelectedVehicle(int? vehicleId) => Update(view => view with { SelectedVehicleId = vehicleId });

    public FleetFrame Filter(FleetFrame frame)
    {
        ViewState view = Volatile.Read(ref _view);
        DateTimeOffset now = frame.PublishedAtUtc;

        // A partial frame cannot express a removal, so the first frame and any roster change go out
        // whole. The client replaces its roster on a full frame and merges a partial one.
        if (_roster is null || !_roster.SetEquals(frame.Vehicles.Select(v => v.VehicleId)))
        {
            _roster = frame.Vehicles.Select(v => v.VehicleId).ToHashSet();
            _lastSentUtc.Clear();
            foreach (VehicleReport report in frame.Vehicles)
            {
                _lastSentUtc[report.VehicleId] = StaggeredSendTime(report.VehicleId, now);
            }

            return frame;
        }

        List<VehicleReport> included = [];
        foreach (VehicleReport report in frame.Vehicles)
        {
            bool inView = report.VehicleId == view.SelectedVehicleId
                || view.Extent is null
                || view.Extent.Contains(report.Longitude, report.Latitude);
            if (!inView && now - _lastSentUtc[report.VehicleId] < OutOfViewInterval) continue;

            _lastSentUtc[report.VehicleId] = now;
            included.Add(report);
        }

        return frame with { Vehicles = included.AsReadOnly(), IsPartial = true };
    }

    /// <summary>Records a full-frame send up to nine tenths of an interval late, by vehicle slot, so
    /// out-of-view vehicles first come due spread across the following second rather than all in
    /// one frame. The first gap is 1.0 to 1.9 s, so no vehicle is ever sent twice within a second.</summary>
    private static DateTimeOffset StaggeredSendTime(int vehicleId, DateTimeOffset sentUtc) =>
        sentUtc + OutOfViewInterval / StaggerSlots * (vehicleId % StaggerSlots);

    private void Update(Func<ViewState, ViewState> change)
    {
        lock (_writeGate)
        {
            Volatile.Write(ref _view, change(_view));
        }
    }
}
