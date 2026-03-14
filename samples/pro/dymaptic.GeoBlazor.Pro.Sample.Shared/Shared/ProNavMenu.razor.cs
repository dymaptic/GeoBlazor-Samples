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

                return new PageLink(href, p.Title, p.IconClass, p.ImageFile);
            }),
        new("imagery-group-blend", "PRO: Imagery Blend", null, "blend.svg", true),
        new("sketch-query", "PRO: Sketch Query", "oi-location", null, true),
        new("edit-feature-data", "PRO: Edit Data", "oi-map-marker", null, true),
        new("popup-edit", "PRO: Popup Edit Data", "oi-pencil", null, true),
        new("update-feature-attributes", "PRO: Update Attributes", "oi-brush", null, true),
        new("apply-edits", "PRO: Apply Edits", "oi-check", null, true),
        new("spatial-relationships", "PRO Relationships", "oi-link-intact", null, true),
        new("demographic-data", "PRO: Demographics", "oi-people", null, true),
        new("length-and-area", "PRO: Length & Area", "oi-graph", null, true),
        new("swipe", "PRO: Swipe Widget", "oi-arrow-thick-left", null, true),
        new("time-slider", "PRO: Time Slider", "oi-vertical-align-center", null, true),
        new("search-custom-source", "PRO: Custom Search", "oi-magnifying-glass", null, true),
        new("clustering", "PRO: Clustering", "oi-fullscreen-exit", null, true),
        new("clustering-popups", "PRO: Clustering Popups", "oi-fullscreen-enter", null, true),
        new("cluster-pie-charts", "PRO: Pie Chart Clusters", "oi-pie-chart", null, true),
        new("binning", "PRO: Binning", "oi-grid-three-up", null, true),
        new("routes", "PRO: Routes", "oi-transfer", null, true),
        new("graphics-legend", "PRO: Graphics Legend", "oi-list-rich", null, true),
        new("group-layers", "PRO: Group Layers", null, "groupLayer.svg", true),
        new("ogc-feature-layers", "PRO: OGC Feature Layers", "oi-layers", null, true),
        new("wfsutils", "PRO: WFS Utils", "oi-wrench", null, true),
        new("print-widget", "PRO: Print Widgets", "oi-print", null, true),
        new("custom-popup-content", "PRO: Custom Popup Content", null, "customPopup.svg", true),
        new("geojson-styles", "PRO: GeoJSON Styles", "oi-brush", null, true),
        new("web-style-symbols", "PRO: Web Style Symbols", "oi-brush", null, true),
        new("highlight-features-by-geometry", "PRO: Highlight by Geometry", "oi-target", null, true)
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