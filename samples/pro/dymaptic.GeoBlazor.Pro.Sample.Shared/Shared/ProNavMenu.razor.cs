using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;


namespace dymaptic.GeoBlazor.Pro.Sample.Shared.Shared;

public partial class ProNavMenu : NavMenu
{
    public override PageLink[] Pages =>
    [
        ..base.Pages
            .Select(p =>
            {
                var href = p.Href;

                if ((p.Href == "widgets") || (p.Href == "bookmarks"))
                {
                    href = $"pro-{href}";
                }

                return p with { Href = href };
            }),
        new("imagery-group-blend", "PRO: Imagery Blend", null, "blend.svg", true, Categories.Layers),
        new("sketch-query", "PRO: Sketch Query", "oi-location", null, true, Categories.Queries),
        new("edit-feature-data", "PRO: Edit Data", "oi-map-marker", null, true, Categories.Interaction),
        new("popup-edit", "PRO: Popup Edit Data", "oi-pencil", null, true, Categories.Widgets),
        new("update-feature-attributes", "PRO: Update Attributes", "oi-brush", null, true, Categories.Interaction),
        new("apply-edits", "PRO: Apply Edits", "oi-check", null, true, Categories.Interaction),
        new("spatial-relationships", "PRO Relationships", "oi-link-intact", null, true, Categories.Queries),
        new("demographic-data", "PRO: Demographics", "oi-people", null, true, Categories.Location),
        new("length-and-area", "PRO: Length & Area", "oi-graph", null, true, Categories.Location),
        new("swipe", "PRO: Swipe Widget", "oi-arrow-thick-left", null, true, Categories.Widgets),
        new("time-slider", "PRO: Time Slider", "oi-vertical-align-center", null, true, Categories.Widgets),
        new("search-custom-source", "PRO: Custom Search", "oi-magnifying-glass", null, true, Categories.Widgets),
        new("clustering", "PRO: Clustering", "oi-fullscreen-exit", null, true, Categories.Visualization),
        new("clustering-popups", "PRO: Clustering Popups", "oi-fullscreen-enter", null, true, Categories.Visualization),
        new("cluster-pie-charts", "PRO: Pie Chart Clusters", "oi-pie-chart", null, true, Categories.Visualization),
        new("binning", "PRO: Binning", "oi-grid-three-up", null, true, Categories.Visualization),
        new("routes", "PRO: Routes", "oi-transfer", null, true, Categories.Location),
        new("graphics-legend", "PRO: Graphics Legend", "oi-list-rich", null, true, Categories.Visualization),
        new("group-layers", "PRO: Group Layers", null, "groupLayer.svg", true, Categories.Layers),
        new("ogc-feature-layers", "PRO: OGC Feature Layers", "oi-layers", null, true, Categories.Layers),
        new("wfsutils", "PRO: WFS Utils", "oi-wrench", null, true, Categories.Layers),
        new("print-widget", "PRO: Print Widgets", "oi-print", null, true, Categories.Widgets),
        new("custom-popup-content", "PRO: Custom Popup Content", null, "customPopup.svg", true, Categories.Widgets),
        new("geojson-styles", "PRO: GeoJSON Styles", "oi-brush", null, true, Categories.Visualization),
        new("web-style-symbols", "PRO: Web Style Symbols", "oi-brush", null, true, Categories.Visualization),
        new("highlight-features-by-geometry", "PRO: Highlight by Geometry", "oi-target", null, true, Categories.Interaction)
    ];

    protected override bool CollapseNavMenu { get; set; } = true;
    private string LowerNavMenuCssClass => _lowerNavMenuOpen ? "" : "lower-collapse";

    private void ToggleLowerNavMenu()
    {
        _lowerNavMenuOpen = !_lowerNavMenuOpen;
    }

    private void ToggleUpperNavMenu()
    {
        CollapseNavMenu = !CollapseNavMenu;
    }

    private bool _lowerNavMenuOpen;
}