using FieldAssetInspector.Razor.Models;

namespace FieldAssetInspector.Razor;

/// <summary>
/// Bridges asset selection events from the Blazor MapPage to the Uno XAML MainPage.
/// Singleton instance shared across the MAUI/Blazor and Uno boundaries.
/// </summary>
public class AssetSelectionService
{
    public static AssetSelectionService Instance { get; } = new();

    public event Action<FieldAsset>? AssetSelected;
    public event Action? SelectionCleared;

    public void Select(FieldAsset asset)
        => AssetSelected?.Invoke(asset);

    public void Clear() => SelectionCleared?.Invoke();
}
