namespace SpatialDispatch.Shared.Contracts;

/// <summary>
///     A service territory identified by the same ID the map layer carries in its feature attributes.
///     Boundary geometry stays in the GeoJSON layer; this business contract carries only the identity.
/// </summary>
public sealed record Territory(int Id, string Name);
