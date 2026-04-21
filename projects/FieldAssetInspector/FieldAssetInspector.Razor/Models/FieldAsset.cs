namespace FieldAssetInspector.Razor.Models;

/// <summary>
/// Represents a field asset (railroad bridge, pipeline, etc.)
/// displayed on the map and inspectable in the sidebar panel.
/// </summary>
public class FieldAsset
{
    public string ObjectIdField { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public Dictionary<string, object?> Attributes { get; set; } = new();
}
