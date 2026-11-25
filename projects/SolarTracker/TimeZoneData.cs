using System.Text.Json.Serialization;
using dymaptic.GeoBlazor.Components.Geometries;

namespace SolarTracker;

public record TimeZoneData(TimeZone[] Features);

public record TimeZone(TimeZoneAttributes Attributes, Polygon Geometry);

public record TimeZoneAttributes
{
    [JsonPropertyName("ZONE")]
    public double Zone { get; init; }
}