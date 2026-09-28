using FleetDashboard.Shared;
using FleetDashboard.Shared.Routing;
using FleetDashboard.Client.Streaming;


namespace FleetDashboard.Client.State;


public sealed record VehicleListItem(int VehicleId, string RouteId, string RouteName,
    double Longitude, double Latitude, double SpeedKph, DateTimeOffset ObservedAtUtc,
    VehicleStatus Status, double AgeSeconds);

public sealed record FleetDashboardView(IReadOnlyList<VehicleListItem> Vehicles,
    VehicleListItem? SelectedVehicle, IReadOnlyList<SpeedSample> SelectedHistory,
    DateTimeOffset? LastFrameAtUtc, bool HasCurrentSnapshot);

public sealed record PendingFleetBatch(IReadOnlyList<VehicleReport> Reports,
    IReadOnlyList<int> RemovedVehicleIds);

public sealed record SpeedSample(DateTimeOffset ObservedAtUtc, double SpeedKph);

public enum VehicleStatus
{
    Moving,
    Idle,
    Stale
}


public static class VehicleListItems
{
    /// <summary>Projects a raw report onto the dashboard's vehicle view, computing status and age
    /// from the observation time relative to the supplied instant.</summary>
    public static VehicleListItem Create(VehicleReport report, DateTimeOffset instant)
    {
        string routeName = FleetRoutes.All.FirstOrDefault(route => route.Id == report.RouteId)?.Name
            ?? report.RouteId;
        VehicleStatus status = instant - report.ObservedAtUtc > TimeSpan.FromSeconds(5)
            ? VehicleStatus.Stale
            : report.SpeedKph > 0 ? VehicleStatus.Moving : VehicleStatus.Idle;
        return new VehicleListItem(report.VehicleId, report.RouteId, routeName, report.Longitude,
            report.Latitude, report.SpeedKph, report.ObservedAtUtc, status,
            Math.Max(0, (instant - report.ObservedAtUtc).TotalSeconds));
    }
}
