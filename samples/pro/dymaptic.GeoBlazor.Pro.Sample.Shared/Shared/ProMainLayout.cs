using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using Microsoft.JSInterop;

namespace dymaptic.GeoBlazor.Pro.Sample.Shared.Shared;

public class ProMainLayout: MainLayout
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            IJSObjectReference coreModule = await AuthenticationManager.GetCoreJsModule();
            await JsRuntime.InvokeVoidAsync("setInterceptors", coreModule);
        }
    }
    
    protected override Type NavMenuType => typeof(ProNavMenu);
}