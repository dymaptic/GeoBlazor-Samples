using FleetDashboard.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace FleetDashboard.Server.Telemetry;

/// <summary>
/// The latest report from each vehicle. It stands in for wherever ingested telemetry lands; a
/// multi-server deployment would use a shared cache such as Redis instead of this in-process one.
/// Entries never expire and the cache has no size limit, so nothing is evicted: a truck that stops
/// reporting keeps its last known report, which is how the dashboard shows it as stale.
/// Safe for one writer (the simulator loop) and any number of concurrent readers.
/// </summary>
public sealed class VehicleReportStore : IDisposable
{
    // A private cache rather than the DI IMemoryCache, so its keys only ever hold vehicle IDs.
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <summary>Replaces the vehicle's stored report with this one.</summary>
    public void Record(VehicleReport report)
    {
        _cache.Set(report.VehicleId, report);
    }

    public void Remove(int vehicleId)
    {
        _cache.Remove(vehicleId);
    }

    /// <summary>Every vehicle's latest report, ordered by vehicle ID.</summary>
    public IReadOnlyList<VehicleReport> Latest()
    {
        List<VehicleReport> reports = new(_cache.Count);
        foreach (object vehicleId in _cache.Keys)
        {
            // A vehicle removed between reading the keys and reading its entry is simply skipped.
            if (_cache.TryGetValue(vehicleId, out VehicleReport? report) && report is not null)
            {
                reports.Add(report);
            }
        }

        reports.Sort(static (left, right) => left.VehicleId.CompareTo(right.VehicleId));
        return reports.AsReadOnly();
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}
