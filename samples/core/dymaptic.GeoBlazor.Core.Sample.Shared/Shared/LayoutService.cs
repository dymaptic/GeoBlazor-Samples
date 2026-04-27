namespace dymaptic.GeoBlazor.Core.Sample.Shared.Shared;

// LayoutStateService.cs
public class LayoutService
{
    public SamplePage? CurrentPage { get; private set; }
    public event Action? OnPageChanged;

    public void SetCurrentPage(SamplePage page)
    {
        CurrentPage = page;
        OnPageChanged?.Invoke();
    }
}