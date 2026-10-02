namespace SpatialDispatch.Shared.Services;

/// <summary>
///     Points the map's GeoJSON layers at the Minimal API instead of the static files.
/// </summary>
/// <remarks>
///     A GeoJSON layer only ever needed a URL, so this is the entire change on the map side: no component
///     edits, no renderer changes, no interop. The static-file source stays in the repository because the
///     earlier checkpoint branches run without a database.
/// </remarks>
public sealed class ApiMapLayerSource(string baseUri) : IMapLayerSource
{
    public string TerritoriesUrl { get; } = Combine(baseUri, "api/layers/territories");

    public string TechniciansUrl { get; } = Combine(baseUri, "api/layers/technicians");

    private static string Combine(string baseUri, string relativePath) =>
        $"{baseUri.TrimEnd('/')}/{relativePath}";
}
