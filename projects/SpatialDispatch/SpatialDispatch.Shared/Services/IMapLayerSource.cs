namespace SpatialDispatch.Shared.Services;

/// <summary>
///     Where the map fetches its GeoJSON feature layers.
/// </summary>
/// <remarks>
///     Keeping the URLs behind an interface is what makes Task 3 a one-line swap: the static files under
///     <c>wwwroot/data</c> become Minimal API endpoints and the GeoBlazor layer wiring is untouched. This is
///     also the boundary the talk states out loud — GeoJSON carries map features, DTOs carry business results.
/// </remarks>
public interface IMapLayerSource
{
    string TerritoriesUrl { get; }

    string TechniciansUrl { get; }
}
