using Microsoft.AspNetCore.Components;

namespace dymaptic.GeoBlazor.Core.Sample.Shared.Shared;

public abstract class SamplePage: ComponentBase
{
    [Inject]
    public required LayoutService LayoutService { get; set; }

    public abstract List<NavMenu.PageLink> PageLinks { get; }

    /// <summary>
    /// A detailed, plain-text description of what this sample demonstrates.
    /// Surfaced both in the rendered page (for users with JavaScript disabled and
    /// for screen readers) and in the page's schema.org JSON-LD metadata so AI
    /// agents and search crawlers can understand the sample without executing JS.
    /// Override on each sample page; default is empty (no description rendered).
    /// </summary>
    public virtual string Description => string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LayoutService.SetCurrentPage(this);
    }
}