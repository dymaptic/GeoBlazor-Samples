using System.Reflection;
using System.Text.Json;

namespace FleetDashboard.Shared.Routing;

/// <summary>A single road-following vertex in longitude/latitude order (WGS84).</summary>
public sealed record RoutePoint(double Longitude, double Latitude);

/// <summary>
/// One prepared fleet path loaded from <c>projects/FleetDashboard/data/routes.geojson</c>. A route
/// is a connected road line; <see cref="AtDistance"/> walks it forward and then back, so the
/// vehicle turns around at the far end instead of following an invented closing segment.
/// </summary>
public sealed class FleetRoute
{
    private readonly RoutePoint[] _points;
    private readonly double[] _segmentLengths;
    private readonly double _lengthKm;

    internal FleetRoute(string id, string name, IReadOnlyList<RoutePoint> points)
    {
        Id = id;
        Name = name;
        _points = RemoveRepeatedPoints(points);
        _segmentLengths = _points.Zip(_points.Skip(1), DistanceKm).ToArray();
        _lengthKm = _segmentLengths.Sum();
        if (_lengthKm <= 0)
        {
            throw new InvalidDataException($"Route '{id}' has zero length.");
        }
    }

    public string Id { get; }
    public string Name { get; }
    public IReadOnlyList<RoutePoint> Points => _points;

    /// <summary>Road length of one one-way pass, in kilometers.</summary>
    public double LengthKm => _lengthKm;

    /// <summary>Position at the given distance from the start, reflecting at the far end.
    /// Distance is the simulator's clock-independent way to move a vehicle along real geometry.</summary>
    public RoutePoint AtDistance(double distanceKm)
    {
        double offset = distanceKm % (2 * _lengthKm);
        if (offset > _lengthKm)
        {
            offset = (2 * _lengthKm) - offset;
        }

        for (int index = 0; index < _segmentLengths.Length; index++)
        {
            if (offset <= _segmentLengths[index] || index == _segmentLengths.Length - 1)
            {
                double ratio = _segmentLengths[index] == 0 ? 0 : offset / _segmentLengths[index];
                RoutePoint start = _points[index];
                RoutePoint end = _points[index + 1];
                return new RoutePoint(start.Longitude + ((end.Longitude - start.Longitude) * ratio),
                    start.Latitude + ((end.Latitude - start.Latitude) * ratio));
            }

            offset -= _segmentLengths[index];
        }

        throw new InvalidOperationException($"Route '{Id}' has no segments.");
    }

    private static RoutePoint[] RemoveRepeatedPoints(IReadOnlyList<RoutePoint> points)
    {
        var result = new List<RoutePoint>(points.Count);
        foreach (RoutePoint point in points)
        {
            if (result.Count == 0 || result[^1] != point)
            {
                result.Add(point);
            }
        }

        if (result.Count < 2)
        {
            throw new InvalidDataException("A fleet route requires at least two distinct points.");
        }

        return [..result];
    }

    private static double DistanceKm(RoutePoint start, RoutePoint end)
    {
        double latitudeRadians = (end.Latitude - start.Latitude) * Math.PI / 180;
        double longitudeRadians = (end.Longitude - start.Longitude) * Math.PI / 180;
        double haversine = Math.Pow(Math.Sin(latitudeRadians / 2), 2)
            + (Math.Cos(start.Latitude * Math.PI / 180) * Math.Cos(end.Latitude * Math.PI / 180)
                * Math.Pow(Math.Sin(longitudeRadians / 2), 2));
        return 12_742 * Math.Asin(Math.Sqrt(haversine));
    }
}

/// <summary>
/// The single source of truth for fleet paths. Loaded once from the GeoJSON embedded in this
/// assembly, so the WebAssembly client and the ASP.NET Core simulator read identical geometry.
/// </summary>
public static class FleetRoutes
{
    /// <summary>Longest allowed gap between adjacent vertices; a larger jump means the prepared
    /// file is corrupt or mixes coordinate orders.</summary>
    public const double MaximumSegmentKm = 5;

    public static IReadOnlyList<FleetRoute> All { get; } = Load();

    private static FleetRoute[] Load()
    {
        Assembly assembly = typeof(FleetRoutes).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("routes.geojson", StringComparison.Ordinal));
        if (resourceName is null)
        {
            throw new InvalidDataException("The embedded routes.geojson resource is missing.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"The embedded routes.geojson resource '{resourceName}' could not be read.");
        using JsonDocument document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("features", out JsonElement features)
            || features.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("routes.geojson must be a GeoJSON FeatureCollection.");
        }

        var routes = new List<FleetRoute>();
        foreach (JsonElement feature in features.EnumerateArray())
        {
            routes.Add(ReadRoute(feature));
        }

        if (routes.Count == 0)
        {
            throw new InvalidDataException("routes.geojson contains no routes.");
        }

        return [..routes];
    }

    private static FleetRoute ReadRoute(JsonElement feature)
    {
        JsonElement properties = feature.GetProperty("properties");
        string id = properties.GetProperty("id").GetString()
            ?? throw new InvalidDataException("A route is missing its id.");
        string name = properties.GetProperty("name").GetString()
            ?? throw new InvalidDataException($"Route '{id}' is missing its name.");
        JsonElement coordinates = feature.GetProperty("geometry").GetProperty("coordinates");
        if (coordinates.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Route '{id}' is not a LineString.");
        }

        var points = new List<RoutePoint>();
        foreach (JsonElement coordinate in coordinates.EnumerateArray())
        {
            double longitude = coordinate[0].GetDouble();
            double latitude = coordinate[1].GetDouble();
            if (longitude is < -180 or > 180 || latitude is < -90 or > 90)
            {
                throw new InvalidDataException($"Route '{id}' has an out-of-range coordinate " +
                    $"({longitude}, {latitude}); coordinates must be longitude/latitude.");
            }

            points.Add(new RoutePoint(longitude, latitude));
        }

        var route = new FleetRoute(id, name, points);

        // Walking the segment lengths is the check that the line is connected and stays local.
        for (int index = 0; index < route.Points.Count - 1; index++)
        {
            double segmentKm = HaversineKm(route.Points[index], route.Points[index + 1]);
            if (segmentKm > MaximumSegmentKm)
            {
                throw new InvalidDataException($"Route '{id}' has a {segmentKm:0.0} km jump between " +
                    $"vertices {index} and {index + 1}; the prepared line is not connected.");
            }
        }

        return route;
    }

    private static double HaversineKm(RoutePoint start, RoutePoint end)
    {
        double latitudeRadians = (end.Latitude - start.Latitude) * Math.PI / 180;
        double longitudeRadians = (end.Longitude - start.Longitude) * Math.PI / 180;
        double haversine = Math.Pow(Math.Sin(latitudeRadians / 2), 2)
            + (Math.Cos(start.Latitude * Math.PI / 180) * Math.Cos(end.Latitude * Math.PI / 180)
                * Math.Pow(Math.Sin(longitudeRadians / 2), 2));
        return 12_742 * Math.Asin(Math.Sqrt(haversine));
    }
}
