using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;


namespace dymaptic.GeoBlazor.Pro.Sample.Shared.Pages;

public partial class SourceCode
{
    [Parameter]
    public string? PageUrl { get; set; }
    [Inject]
    public required HttpClient HttpClient { get; set; }
    [Inject]
    public required IJSRuntime JsRuntime { get; set; }
    [Inject]
    public required IConfiguration Configuration { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (PageUrl is null)
        {
            PageUrl = string.Empty;

            return;
        }

        if (firstRender)
        {
            string docsUrl = Configuration["DocsUrl"] ?? "https://docs.geoblazor.com";
            HttpClient.BaseAddress ??= new Uri(docsUrl);
            string pageUrl = $"assets/samples/{PageUrl}.razor.txt";
            _razorContent = await HttpClient.GetStringAsync(pageUrl);

            // split apart the markup section and the code section so the highlighting can be language-specific
            // for HTML and C#, since there is no widely accepted Razor syntax highlighting
            if (_razorContent.Contains("@code"))
            {
                int codeIndex = _razorContent.IndexOf("@code", StringComparison.Ordinal);
                _codeContent = _razorContent[codeIndex..].Trim();
                _razorContent = _razorContent[..codeIndex].Trim();
            }

            // check for code-behind file
            pageUrl = $"assets/samples/{PageUrl}.razor.cs.txt";

            try
            {
                HttpResponseMessage result = await HttpClient.GetAsync(pageUrl);

                if (result.IsSuccessStatusCode)
                {
                    _codeContent = $"""
                                    ## {PageUrl}.razor.cs
                                    
                                    {await result.Content.ReadAsStringAsync()}
                                    """;

                    _razorContent = $"""
                                    ## {PageUrl}.razor

                                    {_razorContent}
                                    """;
                }
            }
            catch
            {
                // ignore
            }

            StateHasChanged();
        }
    }

    private string _razorContent = string.Empty;
    private string _codeContent = string.Empty;
}