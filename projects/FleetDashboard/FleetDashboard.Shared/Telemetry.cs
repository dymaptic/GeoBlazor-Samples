namespace FleetDashboard.Shared;


public sealed record VehicleReport(
    int VehicleId,
    long Sequence,
    DateTimeOffset ObservedAtUtc,
    double Longitude,
    double Latitude,
    double SpeedKph,
    string RouteId);

/// <summary>One published simulator tick. A frame is the complete roster unless
/// <paramref name="IsPartial"/> is set: viewport-filtered frames carry only the vehicles a client
/// needs this tick, and the client merges them onto the roster it already holds.</summary>
public sealed record FleetFrame(
    Guid RunId,
    long FrameSequence,
    DateTimeOffset PublishedAtUtc,
    IReadOnlyList<VehicleReport> Vehicles,
    bool IsPartial = false);

/// <summary>A visible map area in WGS84 longitude/latitude degrees.</summary>
public sealed record ViewportExtent(double XMin, double YMin, double XMax, double YMax)
{
    public bool Contains(double longitude, double latitude) =>
        longitude >= XMin && longitude <= XMax && latitude >= YMin && latitude <= YMax;
}
