using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace dymaptic.GeoBlazor.Pro.Sample.Shared.Shared;

public class ErrorHandler : ErrorBoundary
{
    [Inject]
    public required NavigationManager Navigation { get; set; }

    protected override Task OnErrorAsync(Exception exception)
    {
        if (exception.Message.Contains("Map component view is in an invalid state."))
        {
            Navigation.Refresh(true);
        }
#if DEBUG
        Console.WriteLine($"Error: {exception.Message}{Environment.NewLine}{exception.StackTrace}");
#endif
        return Task.CompletedTask;
    }
}