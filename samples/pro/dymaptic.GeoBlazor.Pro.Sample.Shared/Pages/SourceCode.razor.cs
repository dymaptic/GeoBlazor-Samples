using ColorCode.Styling;
using Markdig;
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
    private MarkdownPipeline Pipeline => new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSyntaxHighlighting(_isDarkMode ? StyleDictionary.DefaultDark : StyleDictionary.DefaultLight)
        .Build();

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
            _isDarkMode = await JsRuntime.InvokeAsync<bool>("isDarkMode");
            var docsUrl = Configuration["DocsUrl"] ?? "https://docs.geoblazor.com";
            HttpClient.BaseAddress ??= new Uri(docsUrl);
            var pageUrl = $"assets/samples/{PageUrl}.razor.txt";
            var markupContent = await HttpClient.GetStringAsync(pageUrl);

            _pageContent = $"""
                            ```html
                            {markupContent}
                            ```
                            """;

            // split apart the markup section and the code section so the highlighting can be language-specific
            // for HTML and C#, since there is no widely accepted Razor syntax highlighting
            if (markupContent.Contains("@code"))
            {
                var codeIndex = markupContent.IndexOf("@code", StringComparison.Ordinal);
                var codeContent = markupContent[codeIndex..].Trim();
                markupContent = markupContent[..codeIndex].Trim();

                _pageContent = $"""
                                ```html
                                {markupContent}
                                ```

                                ```csharp
                                {codeContent}
                                ```
                                """;
            }
            else
            {
                _pageContent = $"""
                                ```html
                                {markupContent}
                                ```
                                """;
            }

            // check for code-behind file
            pageUrl = $"assets/samples/{PageUrl}.razor.cs.txt";

            try
            {
                var result = await HttpClient.GetAsync(pageUrl);

                if (result.IsSuccessStatusCode)
                {
                    var codeContent = await result.Content.ReadAsStringAsync();

                    _pageContent = $"""
                                    ## {PageUrl}.razor

                                    {_pageContent}

                                    ## {PageUrl}.razor.cs

                                    ```csharp
                                    {codeContent}
                                    ```
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

    private string _pageContent = string.Empty;
    private bool _isDarkMode;
}