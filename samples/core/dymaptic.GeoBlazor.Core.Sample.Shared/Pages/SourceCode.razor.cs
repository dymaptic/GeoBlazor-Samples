using System.Reflection;
using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using Microsoft.AspNetCore.Components;

namespace dymaptic.GeoBlazor.Core.Sample.Shared.Pages;

public partial class SourceCode
{
    [Parameter]
    public string? PageUrl { get; set; }

    [Inject]
    public required ISampleSourceProvider SourceProvider { get; set; }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(PageUrl))
        {
            _razorContent = string.Empty;
            _codeContent = string.Empty;
            _notFound = false;
            return;
        }

        Type? pageType = ResolvePageType(PageUrl);
        if (pageType is null)
        {
            _razorContent = string.Empty;
            _codeContent = string.Empty;
            _notFound = true;
            return;
        }

        (string razor, string? codeBehind) = SourceProvider.GetSource(pageType);

        if (string.IsNullOrEmpty(razor))
        {
            _razorContent = string.Empty;
            _codeContent = string.Empty;
            _notFound = true;
            return;
        }

        _notFound = false;

        // split apart the markup section and the code section so the highlighting can be language-specific
        // for HTML and C#, since there is no widely accepted Razor syntax highlighting
        if (razor.Contains("@code"))
        {
            int codeIndex = razor.IndexOf("@code", StringComparison.Ordinal);
            _codeContent = razor[codeIndex..].Trim();
            _razorContent = razor[..codeIndex].Trim();
        }
        else
        {
            _razorContent = razor;
            _codeContent = string.Empty;
        }

        if (!string.IsNullOrEmpty(codeBehind))
        {
            _razorContent = $"""
                             ## {PageUrl}.razor

                             {_razorContent}
                             """;
            _codeContent = $"""
                            ## {PageUrl}.razor.cs

                            {codeBehind}
                            """;
        }
    }

    private static Type? ResolvePageType(string typeName)
    {
        Dictionary<string, Type> index = LazyPageIndex.Value;
        return index.TryGetValue(typeName, out Type? t) ? t : null;
    }

    private static readonly Lazy<Dictionary<string, Type>> LazyPageIndex = new(() =>
    {
        var index = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? name = assembly.GetName().Name;
            if (name is null) continue;
            if (!name.Contains("Sample.Shared", StringComparison.Ordinal)) continue;

            Type[] types;
            try { types = assembly.GetExportedTypes(); }
            catch { continue; }

            foreach (Type type in types)
            {
                if (type.GetCustomAttributes<RouteAttribute>().Any())
                {
                    // last writer wins; Pro can shadow Core if names collide
                    index[type.Name] = type;
                }
            }
        }
        return index;
    });

    private string _razorContent = string.Empty;
    private string _codeContent = string.Empty;
    private bool _notFound;
}
