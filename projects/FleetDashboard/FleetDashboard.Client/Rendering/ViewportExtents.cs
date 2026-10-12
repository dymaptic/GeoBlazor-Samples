using FleetDashboard.Shared;

namespace FleetDashboard.Client.Rendering;

/// <summary>Converts a map view extent to the WGS84 degrees the server filters in. The view reports
/// its extent in its own spatial reference, which is Web Mercator under the default basemap even
/// though the vehicle layer is WGS84.</summary>
public static class ViewportExtents
{
    private const double EarthRadiusMeters = 6378137d;

    public static ViewportExtent ToGeographic(double xmin, double ymin, double xmax, double ymax, int? wkid)
    {
        bool webMercator = wkid is 3857 or 102100 or 102113 or 900913
            || (wkid is null && Math.Max(Math.Abs(xmin), Math.Abs(xmax)) > 180);
        if (!webMercator) return new ViewportExtent(xmin, ymin, xmax, ymax);

        return new ViewportExtent(ToLongitude(xmin), ToLatitude(ymin), ToLongitude(xmax), ToLatitude(ymax));
    }

    private static double ToLongitude(double x) => x / EarthRadiusMeters * 180d / Math.PI;

    private static double ToLatitude(double y) =>
        (2d * Math.Atan(Math.Exp(y / EarthRadiusMeters)) - Math.PI / 2d) * 180d / Math.PI;
}
