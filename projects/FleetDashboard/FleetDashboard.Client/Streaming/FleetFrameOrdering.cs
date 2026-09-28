using FleetDashboard.Shared;

namespace FleetDashboard.Client.Streaming;

public static class FleetFrameOrdering
{
    public static FleetFrame Newest(FleetFrame current, FleetFrame candidate)
    {
        if (current.RunId != candidate.RunId)
        {
            return current;
        }

        return candidate.FrameSequence > current.FrameSequence ? candidate : current;
    }

    public static FleetFrame ReconcileSnapshot(FleetFrame snapshot, FleetFrame? current, FleetFrame? pending)
    {
        FleetFrame reconciled = current is null ? snapshot : Newest(snapshot, current);
        return pending is null ? reconciled : Newest(reconciled, pending);
    }
}
