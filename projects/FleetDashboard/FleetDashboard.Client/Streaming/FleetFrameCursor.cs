using FleetDashboard.Shared;


namespace FleetDashboard.Client.Streaming;


/// <summary>Tracks the newest frame a consumer has already processed. A frame source keeps
/// serving its last known frame until a new one arrives, so a consumer that counts or queues
/// every delivery must tell a genuine new frame from a repeat of the previous one. Not
/// thread-safe: the dashboard update loop is the only caller.</summary>
public sealed class FleetFrameCursor
{
    /// <summary>Returns true for a frame that has not been seen before, and remembers it.</summary>
    public bool TryAdvance(FleetFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_runId == frame.RunId && frame.FrameSequence <= _frameSequence) return false;

        _runId = frame.RunId;
        _frameSequence = frame.FrameSequence;
        return true;
    }

    private Guid? _runId;
    private long _frameSequence;
}
