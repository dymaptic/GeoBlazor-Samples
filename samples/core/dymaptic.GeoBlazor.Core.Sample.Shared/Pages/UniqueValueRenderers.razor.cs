using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Renderers;
using dymaptic.GeoBlazor.Core.Components.Symbols;
using dymaptic.GeoBlazor.Core.Enums;
using dymaptic.GeoBlazor.Core.Extensions;
using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;


namespace dymaptic.GeoBlazor.Core.Sample.Shared.Pages;

public partial class UniqueValueRenderers
{
    public override List<NavMenu.PageLink> PageLinks =>
    [
        new("https://developers.arcgis.com/javascript/latest/api-reference/esri-renderers-UniqueValueRenderer.html", "ArcGIS Maps SDK for JavaScript"),
        new("https://arcgis.com/home/item.html?id=7afec250e02845868db89c83949a672f", "OpenStreetMap Highways for North America")
    ];

    public override string Description =>
        "This GeoBlazor sample, written in Blazor for .NET developers, demonstrates the UniqueValueRenderer " +
        "from the ArcGIS Maps SDK for JavaScript exposed through GeoBlazor's UniqueValueRenderer, " +
        "UniqueValueInfo, SimpleLineSymbol, and OrderByInfo Razor and C# components. The page shows a 2D " +
        "basemap centered over the Houston, Texas area at a city-block scale, overlaid with an " +
        "OpenStreetMap North America highways FeatureLayer. The renderer color-codes each road by its " +
        "highway attribute, mapping motorways and trunks to thick pink and orange lines, primary roads " +
        "to yellow, secondary to green, tertiary to blue, residential and unclassified streets to neutral " +
        "grays, and pedestrian, footway, path, track, busway, raceway, construction, and proposed roads " +
        "to a variety of dashed earth-tone styles; features are drawn in descending maxspeed order. Below " +
        "the map a Toggle Legend button shows or hides a LegendWidget in the lower-left corner that lists " +
        "every road type and its symbol under the heading Route Type. The sample is intended to " +
        "demonstrate building a rich categorical renderer against a hosted FeatureLayer in a Blazor " +
        "application without writing JavaScript.";

    private static readonly Dictionary<string, SimpleLineSymbol> roadTypes = new()
    {
        // Major highways - wide, bold colors
        ["motorway"] = new SimpleLineSymbol(new MapColor(232, 63, 111), 5, SimpleLineSymbolStyle.Solid),
        ["motorway_link"] = new SimpleLineSymbol(new MapColor(232, 63, 111), 3, SimpleLineSymbolStyle.Solid),
        ["trunk"] = new SimpleLineSymbol(new MapColor(247, 148, 29), 4, SimpleLineSymbolStyle.Solid),
        ["trunk_link"] = new SimpleLineSymbol(new MapColor(247, 148, 29), 2, SimpleLineSymbolStyle.Solid),

        // Primary roads - medium-wide, warm colors
        ["primary"] = new SimpleLineSymbol(new MapColor(255, 200, 69), 4, SimpleLineSymbolStyle.Solid),
        ["primary_link"] = new SimpleLineSymbol(new MapColor(255, 200, 69), 2, SimpleLineSymbolStyle.Solid),

        // Secondary roads - medium width, cooler tones
        ["secondary"] = new SimpleLineSymbol(new MapColor(141, 198, 63), 3, SimpleLineSymbolStyle.Solid),
        ["secondary_link"] = new SimpleLineSymbol(new MapColor(141, 198, 63), 2, SimpleLineSymbolStyle.Solid),

        // Tertiary roads - narrower
        ["tertiary"] = new SimpleLineSymbol(new MapColor(102, 178, 255), 2.5, SimpleLineSymbolStyle.Solid),
        ["tertiary_link"] = new SimpleLineSymbol(new MapColor(102, 178, 255), 1.5, SimpleLineSymbolStyle.Solid),

        // Local roads - thin, neutral colors
        ["residential"] = new SimpleLineSymbol(new MapColor(200, 200, 200), 2, SimpleLineSymbolStyle.Solid),
        ["living_street"] = new SimpleLineSymbol(new MapColor(180, 180, 220), 2, SimpleLineSymbolStyle.Solid),
        ["unclassified"] = new SimpleLineSymbol(new MapColor(170, 170, 170), 1.5, SimpleLineSymbolStyle.Solid),
        ["road"] = new SimpleLineSymbol(new MapColor(150, 150, 150), 1.5, SimpleLineSymbolStyle.Solid),

        // Service and access roads
        ["service"] = new SimpleLineSymbol(new MapColor(128, 128, 128), 1, SimpleLineSymbolStyle.Solid),
        ["services"] = new SimpleLineSymbol(new MapColor(128, 128, 128), 1, SimpleLineSymbolStyle.Solid),

        // Pedestrian paths - thin, dashed styles, earth tones
        ["footway"] = new SimpleLineSymbol(new MapColor(139, 90, 43), 1, SimpleLineSymbolStyle.Dash),
        ["path"] = new SimpleLineSymbol(new MapColor(160, 82, 45), 1, SimpleLineSymbolStyle.Dash),
        ["pedestrian"] = new SimpleLineSymbol(new MapColor(139, 69, 19), 1.5, SimpleLineSymbolStyle.ShortDash),
        ["steps"] = new SimpleLineSymbol(new MapColor(105, 105, 105), 1, SimpleLineSymbolStyle.Dot),
        ["corridor"] = new SimpleLineSymbol(new MapColor(169, 169, 169), 1, SimpleLineSymbolStyle.ShortDash),

        // Off-road and rural paths
        ["track"] = new SimpleLineSymbol(new MapColor(139, 119, 101), 1.5, SimpleLineSymbolStyle.LongDash),
        ["bridleway"] = new SimpleLineSymbol(new MapColor(107, 142, 35), 1, SimpleLineSymbolStyle.DashDot),

        // Special purpose roads
        ["busway"] = new SimpleLineSymbol(new MapColor(0, 112, 192), 2.5, SimpleLineSymbolStyle.Solid),
        ["raceway"] = new SimpleLineSymbol(new MapColor(255, 0, 0), 3, SimpleLineSymbolStyle.Solid),

        // Under development - distinctive dashed patterns
        ["construction"] = new SimpleLineSymbol(new MapColor(255, 165, 0), 2, SimpleLineSymbolStyle.DashDot),
        ["proposed"] = new SimpleLineSymbol(new MapColor(192, 192, 192), 1.5, SimpleLineSymbolStyle.Dot)
    };
    private readonly UniqueValueRenderer _uniqueValueRenderer = new(uniqueValueInfos: roadTypes
            .Select(r => new UniqueValueInfo(string.Concat(r.Key[0].ToString().ToUpperInvariant(), r.Key.AsSpan(1)).Replace("_", " "), r.Value, r.Key))
            .ToArray(),
        field: "highway", defaultLabel: "Service",
        legendOptions: new UniqueValueRendererLegendOptions("Route Type"));
    private bool _showLegend;
}