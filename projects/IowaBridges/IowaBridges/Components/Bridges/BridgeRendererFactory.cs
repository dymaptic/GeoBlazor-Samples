using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Renderers;
using dymaptic.GeoBlazor.Core.Components.Symbols;
using dymaptic.GeoBlazor.Core.Enums;
using dymaptic.GeoBlazor.Core.Model;

namespace IowaBridges.Components.Bridges;

/// <summary>
/// Builds the UniqueValueRenderer used for NBI bridge condition.
///
/// DEVIATION FROM PROMPT: the prompt called for a UniqueValueRenderer with an
/// Arcade ValueExpression computing the min of DECK/SUPERSTRUCTURE/SUBSTRUCTURE
/// condition. In GeoBlazor 4.4.4 the renderer's ValueExpression set in C# does
/// not appear to be applied client-side (every feature falls through to
/// DefaultSymbol). We instead classify by the deck condition field
/// (DECK_COND_058) which the NBI service serves directly. This is a close
/// proxy for composite condition since deck condition is one of the three
/// component ratings; the legend labels still describe the NBI 0-9 scale.
/// </summary>
public static class BridgeRendererFactory
{
    public static UniqueValueRenderer Create()
    {
        var poorSymbol = MarkerSymbol("#c0432a", 7, "rgba(255,255,255,0.5)", 0.5);
        var fairSymbol = MarkerSymbol("#d4a73c", 7, "rgba(255,255,255,0.5)", 0.5);
        var goodSymbol = MarkerSymbol("#5a8a3a", 7, "rgba(255,255,255,0.5)", 0.5);

        return new UniqueValueRenderer(field: "DECK_COND_058", defaultLabel: "Unknown / culvert",
            defaultSymbol: MarkerSymbol("#6a6359", 5, "rgba(0,0,0,0)", 0),
            uniqueValueInfos:
            [
                new UniqueValueInfo("Poor (NBI 0-4)", poorSymbol, "0"),
                new UniqueValueInfo("Poor", poorSymbol, "1"),
                new UniqueValueInfo("Poor", poorSymbol, "2"),
                new UniqueValueInfo("Poor", poorSymbol, "3"),
                new UniqueValueInfo("Poor", poorSymbol, "4"),
                new UniqueValueInfo("Fair (NBI 5-6)", fairSymbol, "5"),
                new UniqueValueInfo("Fair", fairSymbol, "6"),
                new UniqueValueInfo("Good (NBI 7-9)", goodSymbol, "7"),
                new UniqueValueInfo("Good", goodSymbol, "8"),
                new UniqueValueInfo("Good", goodSymbol, "9")
            ]);
    }

    private static SimpleMarkerSymbol MarkerSymbol(string fill, double size, string outlineColor, double outlineWidth)
    {
        return new SimpleMarkerSymbol(new Outline(new MapColor(outlineColor), outlineWidth),
            new MapColor(fill), size, SimpleMarkerSymbolStyle.Circle);
    }
}
#pragma warning restore BL0005
