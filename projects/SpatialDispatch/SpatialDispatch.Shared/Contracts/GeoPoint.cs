namespace SpatialDispatch.Shared.Contracts;

/// <summary>
///     A WGS 84 position in the longitude-first order used by Well-Known Text and GeoJSON.
/// </summary>
/// <remarks>
///     Longitude is deliberately the first parameter. The single most common first-timer mistake in this
///     session's subject area is passing latitude first, which puts Pennsylvania in the Indian Ocean.
/// </remarks>
public readonly record struct GeoPoint(double Longitude, double Latitude);
