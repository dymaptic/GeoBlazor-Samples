namespace FieldAssetInspector.Razor.Models;

/// <summary>
/// Represents a field asset (utility pole, fire hydrant, valve, etc.)
/// displayed on the map and editable in the inspector panel.
/// </summary>
public class FieldAsset
{
    public string ObjectId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FacilityId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Dictionary<string, object?> Attributes { get; set; } = new();
}
