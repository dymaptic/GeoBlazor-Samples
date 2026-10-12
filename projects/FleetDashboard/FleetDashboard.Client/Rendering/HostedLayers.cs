using Microsoft.Extensions.Configuration;

namespace FleetDashboard.Client.Rendering;

public enum HostedLayerKind
{
    /// <summary>A dynamic map service drawn as server-rendered images (<c>MapImageLayer</c>).</summary>
    MapImage,

    /// <summary>A feature service queried by URL (<c>FeatureLayer</c>).</summary>
    Feature
}

/// <summary>One service somebody else hosts and keeps current. The map only refreshes it on an
/// interval; none of the fleet pipeline (the server's frame clock, coalescing, batching) is involved.</summary>
/// <param name="RefreshMinutes">GeoBlazor and ArcGIS JS <c>RefreshInterval</c> are in minutes.</param>
/// <param name="RequiresApiKey">The service rejects anonymous requests, so the layer is offered
/// only when an ArcGIS API key is configured.</param>
public sealed record HostedLayerDefinition(string Id, string Label, string Url, HostedLayerKind Kind,
    double RefreshMinutes, double Opacity, bool RequiresApiKey = false);

/// <summary>The single place the optional live layers are defined.</summary>
public static class HostedLayers
{
    /// <summary>The top-level configuration key GeoBlazor reads for an ArcGIS API key.</summary>
    public const string ApiKeyConfigName = "ArcGISApiKey";

    public static readonly HostedLayerDefinition Radar = new("radar", "NOAA radar",
        "https://mapservices.weather.noaa.gov/eventdriven/rest/services/radar/radar_base_reflectivity/MapServer",
        HostedLayerKind.MapImage, RefreshMinutes: 5, Opacity: 0.6);

    public static readonly HostedLayerDefinition Warnings = new("warnings", "NWS watches and warnings",
        "https://services9.arcgis.com/RHVPKKiFTONKtxq3/arcgis/rest/services/NWS_Watches_Warnings_v1/FeatureServer/6",
        HostedLayerKind.Feature, RefreshMinutes: 3, Opacity: 0.6);

    public static readonly HostedLayerDefinition Traffic = new("traffic", "Esri traffic",
        "https://traffic.arcgis.com/arcgis/rest/services/World/Traffic/MapServer",
        HostedLayerKind.MapImage, RefreshMinutes: 5, Opacity: 1, RequiresApiKey: true);

    public static IReadOnlyList<HostedLayerDefinition> All { get; } = [Radar, Warnings, Traffic];

    public static bool HasApiKey(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration[ApiKeyConfigName]);

    /// <summary>The layers to offer: key-gated ones appear only when a key is configured.</summary>
    public static IReadOnlyList<HostedLayerDefinition> Available(bool hasApiKey) =>
        All.Where(layer => hasApiKey || !layer.RequiresApiKey).ToArray();
}
