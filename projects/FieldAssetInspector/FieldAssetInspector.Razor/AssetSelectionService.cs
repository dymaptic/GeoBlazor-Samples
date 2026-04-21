using FieldAssetInspector.Razor.Models;

namespace FieldAssetInspector.Razor;

/// <summary>
/// Bridges asset selection events from the Blazor MapPage to the Uno XAML MainPage.
/// Registered as a DI singleton so the same instance is injected on both the
/// Blazor (MauiHost → BlazorWebView) and Uno (MainPage) sides of the boundary.
/// </summary>
public class AssetSelectionService
{
    public event Action<FieldAsset>? AssetSelected;
    public event Action? SelectionCleared;

    public void Select(FieldAsset asset)
        => AssetSelected?.Invoke(asset);

    public void Clear() => SelectionCleared?.Invoke();
}
