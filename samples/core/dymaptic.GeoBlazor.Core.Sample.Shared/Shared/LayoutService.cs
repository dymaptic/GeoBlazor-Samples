namespace dymaptic.GeoBlazor.Core.Sample.Shared.Shared;

public class LayoutService
{
    public SamplePage? CurrentPage { get; private set; }

    // URI the CurrentPage was registered for. Consumers should only treat
    // CurrentPage as live when this matches the layout's current URI —
    // that way stale references from a previously-rendered SamplePage are
    // silently ignored after navigation, with no race against component
    // initialization order.
    public string? PageUri { get; private set; }

    public event Action? OnPageChanged;

    public void SetCurrentPage(SamplePage page, string uri)
    {
        CurrentPage = page;
        PageUri = uri;
        OnPageChanged?.Invoke();
    }
}