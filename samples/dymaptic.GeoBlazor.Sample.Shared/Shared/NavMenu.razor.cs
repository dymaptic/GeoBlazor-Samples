using dymaptic.GeoBlazor.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace dymaptic.GeoBlazor.Sample.Shared.Shared;

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
        if (!string.IsNullOrWhiteSpace(_searchText))
            return true;

        return ExpandedGroups.Contains(groupName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // instantiate GeoBlazor modules here so we can initialize our JS and register the views
            IJSObjectReference core = await JsModuleManager.GetCoreJsModule(JsRuntime, CancellationToken.None);
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
        new("ground-elevation", "Ground Elevation", "oi-layers", Category: Categories.MapsAndScenes),

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
        new("imagery-group-blend", "Imagery Blend", null, "blend.svg", Category: Categories.Layers),
        new("group-layers", "Group Layers", null, "groupLayer.svg", Category: Categories.Layers),
        new("ogc-feature-layers", "OGC Feature Layers", "oi-layers", Category: Categories.Layers),
        new("wfsutils", "WFS Utils", "oi-wrench", Category: Categories.Layers),

        new("labels", "Labels", "oi-text", Category: Categories.Visualization),
        new("unique-value", "Unique Renderers", "oi-eyedropper", Category: Categories.Visualization),
        new("marker-rotation", "Marker Rotation", "oi-loop-circular", Category: Categories.Visualization),
        new("clustering", "Clustering", "oi-fullscreen-exit", Category: Categories.Visualization),
        new("clustering-popups", "Clustering Popups", "oi-fullscreen-enter", Category: Categories.Visualization),
        new("cluster-pie-charts", "Pie Chart Clusters", "oi-pie-chart", Category: Categories.Visualization),
        new("binning", "Binning", "oi-grid-three-up", Category: Categories.Visualization),
        new("graphics-legend", "Graphics Legend", "oi-list-rich", Category: Categories.Visualization),
        new("geojson-styles", "GeoJSON Styles", "oi-brush", Category: Categories.Visualization),
        new("web-style-symbols", "Web Style Symbols", "oi-brush", Category: Categories.Visualization),
        new("cim-symbols", "CIM Symbols", "oi-brush", Category: Categories.Visualization),

        new("widgets", "Widgets", "oi-location", Category: Categories.Widgets),
        new("compass-widget", "Compass Widget", "oi-compass", Category: Categories.Widgets),
        new("scale-bar-widget", "Scale Bar Widget", "oi-resize-width", Category: Categories.Widgets),
        new("legends", "Legend", null, "legend.svg", Category: Categories.Widgets),
        new("popups", "Popups", "oi-chat", Category: Categories.Widgets),
        new("popup-actions", "Popup Actions", "oi-bullhorn", Category: Categories.Widgets),
        new("layer-lists", "Layer Lists", "oi-list", Category: Categories.Widgets),
        new("basemap-toggle-widget", "Basemap Toggle", "oi-loop", Category: Categories.Widgets),
        new("basemap-layer-lists", "Basemap Layer Lists", "oi-spreadsheet", Category: Categories.Widgets),
        new("locate-widget", "Locate Widget", "oi-target", Category: Categories.Widgets),
        new("measurement-widgets", "Measurement Widgets", null, "ruler.svg", Category: Categories.Widgets),
        new("search-multi-source", "Search Multiple Sources", "oi-magnifying-glass", Category: Categories.Widgets),
        new("bookmarks", "Bookmarks", "oi-bookmark", Category: Categories.Widgets),
        new("popup-edit", "Popup Edit Data", "oi-pencil", Category: Categories.Widgets),
        new("swipe", "Swipe Widget", "oi-arrow-thick-left", Category: Categories.Widgets),
        new("new-widgets", "New Widgets", "oi-layers", Category: Categories.Widgets),
        new("time-slider", "Time Slider", "oi-vertical-align-center", Category: Categories.Widgets),
        new("search-custom-source", "Custom Search", "oi-magnifying-glass", Category: Categories.Widgets),
        new("print-widget", "Print Widgets", "oi-print", Category: Categories.Widgets),
        new("custom-popup-content", "Custom Popup Content", null, "customPopup.svg", Category: Categories.Widgets),
        new("feature-tables", "Feature Tables", null, "featureTable.svg", Category: Categories.Widgets),

        new("sql-query", "SQL Query", "oi-data-transfer-download", Category: Categories.Queries),
        new("sql-filter-query", "SQL Filter", "oi-arrow-thick-bottom", Category: Categories.Queries),
        new("server-side-queries", "Server-Side Queries", "oi-question-mark", Category: Categories.Queries),
        new("query-related-features", "Query Related Features", "oi-people", Category: Categories.Queries),
        new("query-top-features", "Query Top Features", "oi-arrow-thick-top", Category: Categories.Queries),
        new("sketch-query", "Sketch Query", "oi-location", Category: Categories.Queries),
        new("spatial-relationships", "Relationships", "oi-link-intact", Category: Categories.Queries),

        new("drawing", "Drawing", "oi-pencil", Category: Categories.Interaction),
        new("click-to-add", "Click to Add Point", "oi-map-marker", Category: Categories.Interaction),
        new("many-graphics", "Many Graphics", "oi-calculator", Category: Categories.Interaction),
        new("events", "Events", "oi-flash", Category: Categories.Interaction),
        new("reactive-utils", "Reactive Utils", "oi-wrench", Category: Categories.Interaction),
        new("hit-tests", "Hit Tests", "oi-star", Category: Categories.Interaction),
        new("graphic-tracking", "Graphic Tracking", "oi-move", Category: Categories.Interaction),
        new("edit-feature-data", "Edit Data", "oi-map-marker", Category: Categories.Interaction),
        new("multi-feature-editing", "Edit Several Features", "oi-layers", Category: Categories.Interaction),
        new("update-feature-attributes", "Update Attributes", "oi-brush", Category: Categories.Interaction),
        new("apply-edits", "Apply Edits", "oi-check", Category: Categories.Interaction),
        new("sketch-as-editor", "Sketch as Editor", null, "sketchEditor.svg", Category: Categories.Interaction),
        new("highlight-features-by-geometry", "Highlight by Geometry", "oi-target", Category: Categories.Interaction),

        new("place-selector", "Place Selector", "oi-arrow-bottom", Category: Categories.Location),
        new("calculate-geometries", "Calculate Geometries", "oi-clipboard", Category: Categories.Location),
        new("projection", "Display Projection", "oi-sun", Category: Categories.Location),
        new("projection-tool", "Projection Tool", "oi-cog", Category: Categories.Location),
        new("basemap-projections", "Basemap Projections", "oi-bullhorn", Category: Categories.Location),
        new("geometry-methods", "Geometry Methods", "oi-task", Category: Categories.Location),
        new("locator-methods", "Locator Methods", "oi-task", Category: Categories.Location),
        new("reverse-geolocator", "GeoLocator", "oi-arrow-circle-bottom", Category: Categories.Location),
        new("service-areas", "Service Areas", "oi-comment-square", Category: Categories.Location),
        new("demographic-data", "Demographics", "oi-people", Category: Categories.Location),
        new("length-and-area", "Length & Area", "oi-graph", Category: Categories.Location),
        new("routes", "Routes", "oi-transfer", Category: Categories.Location),
    ];

    public record PageLink(
        string Href, string Title, string? IconClass = null,
        string? ImageFile = null, string? Category = null);
}