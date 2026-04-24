using Microsoft.AspNetCore.Components;

namespace dymaptic.GeoBlazor.Core.Sample.Shared.Shared;

public abstract class SamplePage: ComponentBase
{
    [Inject]
    public required LayoutService LayoutService { get; set; }
    
    public abstract List<NavMenu.PageLink> PageLinks { get; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LayoutService.SetCurrentPage(this);
    }
}