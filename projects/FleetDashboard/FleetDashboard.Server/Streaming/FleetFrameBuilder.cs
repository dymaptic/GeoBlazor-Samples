using FleetDashboard.Server.Telemetry;
using FleetDashboard.Shared;

namespace FleetDashboard.Server.Streaming;

/// <summary>
/// The server's job of turning stored telemetry into a frame. Each frame is a complete snapshot of
/// every vehicle's latest report, including the ones that have not changed, so a client that misses
/// a frame loses nothing. Frames are stamped with the run, a frame sequence, and the publish time,
/// none of which the vehicles themselves know about.
/// </summary>
public sealed class FleetFrameBuilder
{
    private readonly object _gate = new();
    private readonly VehicleReportStore _store;
    private readonly TimeProvider _timeProvider;
    private Guid _runId = Guid.NewGuid();
    private long _frameSequence;
    private FleetFrame _currentFrame;

    public FleetFrameBuilder(VehicleReportStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
        _currentFrame = new FleetFrame(_runId, 0, timeProvider.GetUtcNow(), []);
    }

    /// <summary>The most recently built frame, served to clients that need a snapshot. Before the
    /// first frame it is an empty frame with sequence 0.</summary>
    public FleetFrame CurrentFrame => Volatile.Read(ref _currentFrame);

    public FleetFrame CreateFrame()
    {
        lock (_gate)
        {
            FleetFrame frame = new(_runId, ++_frameSequence, _timeProvider.GetUtcNow(), _store.Latest());
            Volatile.Write(ref _currentFrame, frame);
            return frame;
        }
    }

    /// <summary>Starts a new run so the next frame is frame 1 under a new run ID. The store and
    /// <see cref="CurrentFrame"/> are left as they are until the next frame is built.</summary>
    public void StartNewRun()
    {
        lock (_gate)
        {
            _runId = Guid.NewGuid();
            _frameSequence = 0;
        }
    }
}
