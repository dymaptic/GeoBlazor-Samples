namespace SpatialDispatch.Shared.Services;

/// <summary>
///     Serves the map's feature layers from GeoJSON files shipped with the application.
/// </summary>
/// <remarks>
///     Task 3 swaps this for a source pointing at the Minimal API's GeoJSON endpoints. Nothing else about the
///     map changes, because a GeoJSON layer only ever needed a URL.
/// </remarks>
public sealed class StaticFileMapLayerSource(string baseUri) : IMapLayerSource
{
    public string TerritoriesUrl { get; } = Combine(baseUri, "data/territories.geojson");

    public string TechniciansUrl { get; } = Combine(baseUri, "data/technicians.geojson");

    private static string Combine(string baseUri, string relativePath) =>
        $"{baseUri.TrimEnd('/')}/{relativePath}";
}
