using System.Collections.Concurrent;

namespace FleetDashboard.Shared.Simulation;

/// <summary>
/// The latest report from each vehicle. It stands in for wherever ingested telemetry lands; a
/// multi-server deployment would use a shared cache such as Redis instead of this in-process one.
/// Entries never expire, so nothing is evicted: a truck that stops reporting keeps its last known
/// report, which is how the dashboard shows it as stale. Safe for one writer (the simulator) and any
/// number of concurrent readers. A plain dictionary rather than a memory cache, so the store needs no
/// package and runs in the browser as well as on the server.
/// </summary>
public sealed class VehicleReportStore
{
    private readonly ConcurrentDictionary<int, VehicleReport> _reports = new();

    /// <summary>Replaces the vehicle's stored report with this one.</summary>
    public void Record(VehicleReport report)
    {
        _reports[report.VehicleId] = report;
    }

    public void Remove(int vehicleId)
    {
        _reports.TryRemove(vehicleId, out _);
    }

    /// <summary>Every vehicle's latest report, ordered by vehicle ID.</summary>
    public IReadOnlyList<VehicleReport> Latest()
    {
        List<VehicleReport> reports = [.. _reports.Values];
        reports.Sort(static (left, right) => left.VehicleId.CompareTo(right.VehicleId));
        return reports.AsReadOnly();
    }
}