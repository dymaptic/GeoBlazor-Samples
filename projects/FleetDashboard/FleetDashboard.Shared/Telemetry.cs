namespace FleetDashboard.Shared;


public sealed record VehicleReport(int VehicleId, long Sequence, DateTimeOffset ObservedAtUtc,
    double Longitude, double Latitude, double SpeedKph, string RouteId);

public sealed record FleetFrame(Guid RunId, long FrameSequence, DateTimeOffset PublishedAtUtc,
    IReadOnlyList<VehicleReport> Vehicles);
