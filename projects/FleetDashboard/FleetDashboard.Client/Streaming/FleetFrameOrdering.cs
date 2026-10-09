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

        if (candidate.FrameSequence <= current.FrameSequence)
        {
            return current;
        }

        return candidate.IsPartial ? MergePartial(current, candidate) : candidate;
    }

    public static FleetFrame ReconcileSnapshot(FleetFrame snapshot, FleetFrame? current, FleetFrame? pending)
    {
        FleetFrame reconciled = current is null ? snapshot : Newest(snapshot, current);
        return pending is null ? reconciled : Newest(reconciled, pending);
    }

    /// <summary>A viewport-filtered frame carries only some vehicles, so it updates the roster it
    /// lands on instead of replacing it. The result keeps the candidate's run and sequence, and is
    /// complete exactly when the frame it landed on was complete.</summary>
    private static FleetFrame MergePartial(FleetFrame current, FleetFrame candidate)
    {
        Dictionary<int, VehicleReport> vehicles = current.Vehicles.ToDictionary(v => v.VehicleId);
        foreach (VehicleReport report in candidate.Vehicles)
        {
            if (!vehicles.TryGetValue(report.VehicleId, out VehicleReport? known) || report.Sequence > known.Sequence)
            {
                vehicles[report.VehicleId] = report;
            }
        }

        VehicleReport[] merged = vehicles.Values.OrderBy(v => v.VehicleId).ToArray();
        return candidate with { Vehicles = Array.AsReadOnly(merged), IsPartial = current.IsPartial };
    }
}
