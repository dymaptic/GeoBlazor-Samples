using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Components.Layers;
using dymaptic.GeoBlazor.Core.Components.Views;
using dymaptic.GeoBlazor.Core.Components.Widgets;
using dymaptic.GeoBlazor.Core.Enums;
using dymaptic.GeoBlazor.Core.Events;
using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Core.Options;
using dymaptic.GeoBlazor.Core.Results;
using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Pro.Events;
using dymaptic.GeoBlazor.Pro.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace dymaptic.GeoBlazor.Pro.Sample.Shared.Pages;

public partial class CustomPopupContents
{
    public override List<NavMenu.PageLink> PageLinks =>
    [
        new("https://developers.arcgis.com/javascript/latest/sample-code/popup-customcontent/", "ArcGIS Maps SDK for JavaScript"),
        new("https://services.arcgis.com/V6ZHFr6zdgNZuVG0/arcgis/rest/services/OverlaySchools/FeatureServer/0", "Private and Public US Schools")
    ];

    public override string Description =>
        "This GeoBlazor Pro sample, written in Blazor for .NET developers, demonstrates custom PopupTemplate content " +
        "elements from the ArcGIS Maps SDK for JavaScript exposed through GeoBlazor's CustomPopupContent, PopupWidget, " +
        "and SearchWidget Razor components. The page shows a 2D topographic map of the contiguous United States " +
        "displaying a FeatureLayer of public and private schools by state. Clicking a state opens a docked popup " +
        "(pinned to the right edge) containing three custom content blocks: a GeoBlazor logo image at the top, an " +
        "embedded SearchWidget that lets the user search by state name within the layer, and a dynamically generated " +
        "HTML block that runs a server-side QueryService statistics query against a private-schools service and " +
        "summarizes elementary, secondary, and combined private-school counts and average enrollment for the " +
        "selected state. The sample is intended to demonstrate how to compose images, widgets, and code-driven HTML " +
        "inside a single popup using the CreatorFunction extension point.";

    [Inject]
    public required QueryService QueryService { get; set; }
    
    [Inject]
    public required IJSRuntime JsRuntime { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            bool isDarkMode = await JsRuntime.InvokeAsync<bool>("isDarkMode");

            if (isDarkMode)
            {
                _logoUrl = "_content/dymaptic.GeoBlazor.Core.Sample.Shared/images/GeoBlazor_by_dymaptic-Logo-400px.webp";
            }
        }
    }

    private async Task<string> GenerateContent(PopupTemplateCreatorEvent creatorEvent)
    {
        try
        {
            StatisticDefinition levelCount = new("LEVEL_", "level_count",
                StatisticType.Count, new StatisticDefinitionStatisticParameters());

            StatisticDefinition enrollmentAvg = new("ENROLLMENT", "enroll_avg",
                StatisticType.Avg, new StatisticDefinitionStatisticParameters());

            Query queryObject = new(Geometry: creatorEvent.Graphic?.Geometry,
                GroupByFieldsForStatistics: ["LEVEL_"],
                OutFields: ["*"], SpatialRelationship: SpatialRelationship.Intersects,
                OutStatistics: [levelCount, enrollmentAvg]);

            FeatureSet? result = await QueryService.ExecuteQueryJSON(_queryUrl, queryObject);

            if (result?.Features is null)
            {
                return string.Empty;
            }

            List<AttributesDictionary> featureAttributes = result
                .Features.Select(f => f.Attributes)
                .ToList();

            foreach (AttributesDictionary attributes in featureAttributes)
            {
                switch (attributes["LEVEL_"])
                {
                    case "Elementary":
                        _stats.ElementaryCount = attributes["level_count"] as int? ?? 0;
                        _stats.ElementaryAverageEnrollment = attributes["enroll_avg"] as double? ?? 0;

                        break;
                    case "Secondary":
                        _stats.SecondaryCount = attributes["level_count"] as int? ?? 0;
                        _stats.SecondaryAverageEnrollment = attributes["enroll_avg"] as double? ?? 0;

                        break;
                    case "Combined elementary and secondary":
                        _stats.TotalCount = attributes["level_count"] as int? ?? 0;
                        _stats.TotalAverageEnrollment = attributes["enroll_avg"] as double? ?? 0;

                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        return $"""
                There is a total of <b>{_stats.ElementaryCount + _stats.SecondaryCount + _stats.TotalCount}</b> 
                private schools that reside within the state. Out of this total amount of private schools: 
                <ul><li><b>{_stats.ElementaryCount}</b> were classified as elementary, with an average enrollment of 
                <b>{_stats.ElementaryAvgFormatted}</b> students.</li>
                <li><b>{_stats.SecondaryCount}</b> were classified as secondary, with an average enrollment of 
                <b>{_stats.SecondaryAvgFormatted}</b> students.</li>
                <li><b>{_stats.TotalCount
                }</b> were classified as both elementary and secondary, with an average enrollment of 
                <b>{_stats.TotalAvgFormatted} </b>students.</li></ul>
                """;
    }

    private async Task OnSelectResult(SearchSelectResultEvent searchSelectResultEvent)
    {
        Graphic? resultGraphic = searchSelectResultEvent.Result?.Feature;
        if (resultGraphic is not null)
        {
            // we have to reload the graphic to ensure that the popup has refreshed and can be reused
            // if you check the ArcGIS JS sample, you can see that it won't work after the first search
            HitTestResult hitTestResult = await _mapView!.HitTest(((Polygon)resultGraphic.Geometry!).Centroid!);

            await _popupWidget!.Open(new PopupOpenOptions(Features: hitTestResult.Results
                .Where(r => r is GraphicHit)
                .Select(g => ((GraphicHit)g).Graphic)
                .ToArray(), ShouldFocus: true));
        }

    }

    private MapView? _mapView;
    private FeatureLayer? _schoolsLayer;
    private PopupWidget? _popupWidget;
    private readonly SchoolStatistics _stats = new();
    private readonly string _queryUrl =
        "https://services.arcgis.com/V6ZHFr6zdgNZuVG0/arcgis/rest/services/PrivateSchools/FeatureServer/0";
    private string _logoUrl = "_content/dymaptic.GeoBlazor.Core.Sample.Shared/images/GeoBlazor_by_dymaptic-Logo-400px-dark.webp";

    private record SchoolStatistics
    {
        public int ElementaryCount { get; set; }
        public int SecondaryCount { get; set; }
        public int TotalCount { get; set; }
        public double ElementaryAverageEnrollment { get; set; }
        public double SecondaryAverageEnrollment { get; set; }
        public double TotalAverageEnrollment { get; set; }
        public string ElementaryAvgFormatted =>
            ElementaryAverageEnrollment.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        public string SecondaryAvgFormatted =>
            SecondaryAverageEnrollment.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        public string TotalAvgFormatted =>
            TotalAverageEnrollment.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}