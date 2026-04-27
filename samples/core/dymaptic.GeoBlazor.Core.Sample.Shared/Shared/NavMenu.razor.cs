using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace dymaptic.GeoBlazor.Core.Sample.Shared.Shared;

public partial class NavMenu
{
    // Category name constants
    public static class Categories
    {
        public const string MapsAndScenes = "Maps & Scenes";
        public const string Layers = "Layers";
        public const string Visualization = "Visualization";
        public const string Widgets = "Widgets";
        public const string Queries = "Queries";
        public const string Interaction = "Interaction";
        public const string Location = "Location";
    }

    protected static readonly string[] GroupOrder =
        [Categories.MapsAndScenes, Categories.Layers, Categories.Visualization,
         Categories.Widgets, Categories.Queries, Categories.Interaction, Categories.Location];

    [Inject]
    public required IJSRuntime JsRuntime { get; set; }
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    [Inject]
    public required JsModuleManager JsModuleManager { get; set; }

    private string? NavMenuCssClass => CollapseNavMenu ? "lower-collapse" : null;
    private string? GlobalNavMenuCssClass => CollapseGlobalNavMenu ? "upper-collapse" : null;

    private IEnumerable<PageLink> FilteredPages => string.IsNullOrWhiteSpace(_searchText)
        ? Pages
        : Pages.Where(p => p.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    protected IEnumerable<PageLink> UngroupedPages => FilteredPages.Where(p => p.Category is null);

    protected IEnumerable<(string GroupName, IEnumerable<PageLink> Pages)> GroupedFilteredPages =>
        GroupOrder
            .Select(g => (GroupName: g, Pages: FilteredPages.Where(p => p.Category == g)))
            .Where(g => g.Pages.Any());

    protected HashSet<string> ExpandedGroups { get; set; } = new();

    protected void ToggleGroup(string groupName)
    {
        if (!ExpandedGroups.Add(groupName))
        {
            ExpandedGroups.Remove(groupName);
        }
    }

    protected bool IsGroupExpanded(string groupName)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
            return true;

        return ExpandedGroups.Contains(groupName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // instantiate GeoBlazor modules here so we can initialize our JS and register the views
            IJSObjectReference? pro = await JsModuleManager.GetProJsModule(JsRuntime, CancellationToken.None);
            IJSObjectReference core = await JsModuleManager.GetCoreJsModule(JsRuntime, pro, CancellationToken.None);
            await JsRuntime.InvokeVoidAsync("initializeGeoBlazor", core);

            string currentPage = NavigationManager
                .ToBaseRelativePath(NavigationManager.Uri)
                .Replace("source-code/", "");

            if (currentPage != string.Empty)
            {
                string? group = Pages.FirstOrDefault(p => p.Href == currentPage)?.Category;

                if (group is not null)
                {
                    ExpandedGroups.Add(group);
                }

                await JsRuntime.InvokeVoidAsync("scrollToNav", currentPage);
            }

            StateHasChanged();
        }
    }
    
    protected void OnSearchFilter(string searchText)
    {
        _searchText = searchText;
        CollapseNavMenu = false;
        StateHasChanged();
    }
    
    protected void OnFullSearch(string searchText)
    {
        if (FilteredPages.Count() == 1)
        {
            CollapseNavMenu = true;
            NavigationManager.NavigateTo(FilteredPages.First().Href);
        }
    }

    protected void ToggleNavMenu()
    {
        CollapseNavMenu = !CollapseNavMenu;
    }
    
    protected void ToggleGlobalNavMenu()
    {
        CollapseGlobalNavMenu = !CollapseGlobalNavMenu;
    }

    protected async Task NavigateTo(string href)
    {
        if (href == NavigationManager.ToBaseRelativePath(NavigationManager.Uri))
        {
            // If the user clicks on the current page, we don't want to navigate away.
            return;
        }

        await JsRuntime.InvokeVoidAsync("setWaitCursor", true);

        await InvokeAsync(async () =>
        {
            NavigationManager.NavigateTo(href);
            await JsRuntime.InvokeVoidAsync("setWaitCursor", false);
            StateHasChanged();
        });
    }

    protected bool CollapseNavMenu { get; set; } = true;
    protected bool CollapseGlobalNavMenu { get; set; } = true;
    protected ElementReference? Navbar { get; set; }
    private string _searchText = string.Empty;
    
    public virtual PageLink[] Pages =>
    [
        new("", "Home", "oi-home"),
        new("navigation", "Navigation", "oi-compass", Category: Categories.MapsAndScenes),
        new("scene", "Scene & Attributes", "oi-globe", Category: Categories.MapsAndScenes),
        new("basemaps", "Basemaps", "oi-map", Category: Categories.MapsAndScenes),
        new("web-map", "Web Map", "oi-browser", Category: Categories.MapsAndScenes),
        new("web-scene", "Web Scene", "oi-box", Category: Categories.MapsAndScenes),

        new("feature-layers", "Feature Layers", "oi-layers", Category: Categories.Layers),
        new("map-image-layers", "Map Image Layers", "oi-image", Category: Categories.Layers),
        new("vector-layer", "Vector Layer", "oi-arrow-right", Category: Categories.Layers),
        new("csv-layer", "CSV Layers", "oi-grid-four-up", Category: Categories.Layers),
        new("kmllayers", "KML Layers", "oi-excerpt", Category: Categories.Layers),
        new("geojson-layers", "GeoJSON Layers", null, "geojson.svg", Category: Categories.Layers),
        new("georss-layer", "GeoRSS Layer", "oi-rss", Category: Categories.Layers),
        new("osm-layer", "OpenStreetMaps Layer", null, "osm.webp", Category: Categories.Layers),
        new("wcslayers", "WCS Layers", "oi-project", Category: Categories.Layers),
        new("wfslayers", "WFS Layers", null, "wfs.svg", Category: Categories.Layers),
        new("wmslayers", "WMS Layers", null, "wms.svg", Category: Categories.Layers),
        new("wmtslayers", "WMTS Layers", null, "wmts.svg", Category: Categories.Layers),
        new("imagerylayer", "Imagery Layers", "oi-image", Category: Categories.Layers),
        new("imagery-tile-layer", "Imagery Tile Layers", null, "tile.webp", Category: Categories.Layers),

        new("labels", "Labels", "oi-text", Category: Categories.Visualization),
        new("unique-value", "Unique Renderers", "oi-eyedropper", Category: Categories.Visualization),
        new("marker-rotation", "Marker Rotation", "oi-loop-circular", Category: Categories.Visualization),

        new("widgets", "Widgets", "oi-location", Category: Categories.Widgets),
        new("popups", "Popups", "oi-chat", Category: Categories.Widgets),
        new("popup-actions", "Popup Actions", "oi-bullhorn", Category: Categories.Widgets),
        new("bookmarks", "Bookmarks", "oi-bookmark", Category: Categories.Widgets),
        new("layer-lists", "Layer Lists", "oi-list", Category: Categories.Widgets),
        new("legends", "Legend", null, "legend.svg", Category: Categories.Widgets),
        new("basemap-layer-lists", "Basemap Layer Lists", "oi-spreadsheet", Category: Categories.Widgets),
        new("measurement-widgets", "Measurement Widgets", null, "ruler.svg", Category: Categories.Widgets),
        new("search-multi-source", "Search Multiple Sources", "oi-magnifying-glass", Category: Categories.Widgets),

        new("sql-query", "SQL Query", "oi-data-transfer-download", Category: Categories.Queries),
        new("sql-filter-query", "SQL Filter", "oi-arrow-thick-bottom", Category: Categories.Queries),
        new("server-side-queries", "Server-Side Queries", "oi-question-mark", Category: Categories.Queries),
        new("query-related-features", "Query Related Features", "oi-people", Category: Categories.Queries),
        new("query-top-features", "Query Top Features", "oi-arrow-thick-top", Category: Categories.Queries),

        new("drawing", "Drawing", "oi-pencil", Category: Categories.Interaction),
        new("click-to-add", "Click to Add Point", "oi-map-marker", Category: Categories.Interaction),
        new("many-graphics", "Many Graphics", "oi-calculator", Category: Categories.Interaction),
        new("events", "Events", "oi-flash", Category: Categories.Interaction),
        new("reactive-utils", "Reactive Utils", "oi-wrench", Category: Categories.Interaction),
        new("hit-tests", "Hit Tests", "oi-star", Category: Categories.Interaction),
        new("graphic-tracking", "Graphic Tracking", "oi-move", Category: Categories.Interaction),

        new("place-selector", "Place Selector", "oi-arrow-bottom", Category: Categories.Location),
        new("service-areas", "Service Areas", "oi-comment-square", Category: Categories.Location),
        new("calculate-geometries", "Calculate Geometries", "oi-clipboard", Category: Categories.Location),
        new("projection", "Display Projection", "oi-sun", Category: Categories.Location),
        new("projection-tool", "Projection Tool", "oi-cog", Category: Categories.Location),
        new("basemap-projections", "Basemap Projections", "oi-bullhorn", Category: Categories.Location),
        new("geometry-methods", "Geometry Methods", "oi-task", Category: Categories.Location),
        new("locator-methods", "Locator Methods", "oi-task", Category: Categories.Location),
        new("reverse-geolocator", "GeoLocator", "oi-arrow-circle-bottom", Category: Categories.Location),
    ];

    public record PageLink(
        string Href, string Title, string? IconClass = null,
        string? ImageFile = null, bool Pro = false, string? Category = null);
}