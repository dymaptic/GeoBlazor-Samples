using dymaptic.GeoBlazor.Core.Components;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Components.Views;
using dymaptic.GeoBlazor.Core.Components.Widgets;
using dymaptic.GeoBlazor.Core.Events;
using dymaptic.GeoBlazor.Core.Model;
using dymaptic.GeoBlazor.Core.Options;
using dymaptic.GeoBlazor.Core.Sample.Shared.Shared;
using dymaptic.GeoBlazor.Pro.Components.Layers;
using Microsoft.AspNetCore.Components;


namespace dymaptic.GeoBlazor.Pro.Sample.Shared.Pages;

public partial class StyledGeoJSONLayers
{
    public override List<NavMenu.PageLink> PageLinks =>
    [
        new("https://wiki.openstreetmap.org/wiki/Geojson_CSS", "GeoJSON CSS Spec"),
        new("https://github.com/mapbox/simplestyle-spec/tree/master/1.1.0", "Mapbox SimpleStyle Spec")
    ];

    [Inject]
    public required GeometryEngine GeometryEngine { get; set; }

    protected async Task OnViewRendered()
    {
        _initialExtent ??= await _mapView!.GetExtent();
    }
    
    private async Task OnLayerViewCreate(LayerViewCreateEvent createEvent)
    {
        if (createEvent.Layer is ProGeoJSONLayer layer)
        {
            FeatureSet? results = await layer
                .QueryFeatures(new Query(Where: "1=1", OutFields: ["*"], ReturnGeometry: true));

            if (results?.Features is not null && results.Features.Count > 0)
            {
                _features[layer] = results.Features.ToList();
            }
            _layerViews[layer] = (GeoJSONLayerView)createEvent.LayerView!;
        }
    }

    private async Task GoToLocation(Graphic feature)
    {
        try
        {
            if (_highlightHandle is not null)
            {
                await _highlightHandle.Remove();
            }

            GeoJSONLayerView layerView = _layerViews[(ProGeoJSONLayer)feature.Layer!];
            _highlightHandle = await layerView.Highlight(feature);
            // create a buffer around the feature geometry
            Polygon buffer = (await GeometryEngine.GeodesicBuffer(feature.Geometry!, 100))!;
            await _popupWidget!.Close();
            await _mapView!.GoTo(_initialExtent!);
            await Task.Delay(200);
            await _mapView.GoTo(buffer.Extent!);
            await _popupWidget.Open(new PopupOpenOptions(Features: [feature], UpdateLocationEnabled: true));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private async Task ToggleLayerVisibility(string layerId)
    {
        switch (layerId)
        {
            case "points":
                await _pointsLayer!.SetVisibility(!_pointsLayer!.Visible!.Value);
                break;
            case "polygons":
                await _polygonsLayer!.SetVisibility(!_polygonsLayer!.Visible!.Value);
                break;
            case "lines":
                await _linesLayer!.SetVisibility(!_linesLayer!.Visible!.Value);
                break;
            case "multipoints":
                await _multipointsLayer!.SetVisibility(!_multipointsLayer!.Visible!.Value);
                break;
        }
    }

    private Dictionary<ProGeoJSONLayer, List<Graphic>> _features = [];
    private Dictionary<ProGeoJSONLayer, GeoJSONLayerView> _layerViews = [];
    private Extent? _initialExtent;
    private Handle? _highlightHandle;
    private MapView? _mapView;
    private PopupWidget? _popupWidget;
    private ProGeoJSONLayer? _pointsLayer;
    private ProGeoJSONLayer? _polygonsLayer;
    private ProGeoJSONLayer? _linesLayer;
    private ProGeoJSONLayer? _multipointsLayer;
    
    // Points layer: Tourist attractions
    private readonly string _pointsJSON =
        """
        {
          "type": "FeatureCollection",
          "style": {
            "title": "Seattle Tourist Attractions",
            "description": "Popular landmarks and attractions in Seattle, Washington"
          },
          "features": [
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3493, 47.6205]
              },
              "properties": {
                "name": "Space Needle",
                "type": "landmark",
                "height": "605 ft",
                "year_built": 1962,
                "marker-color": "#ff6b35",
                "marker-size": "large",
                "marker-symbol": "star",
                "title": "Space Needle",
                "description": "Seattle's most iconic landmark built for the 1962 World's Fair"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3421, 47.6089]
              },
              "properties": {
                "name": "Pike Place Market",
                "type": "market",
                "established": 1907,
                "marker-color": "#ffd23f",
                "marker-size": "medium",
                "marker-symbol": "shop",
                "title": "Pike Place Market",
                "description": "Historic farmers market and one of the oldest continuously operated public farmers' markets in the United States"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3331, 47.5979]
              },
              "properties": {
                "name": "Seattle Art Museum",
                "type": "museum",
                "marker-color": "#7b68ee",
                "marker-size": "medium",
                "marker-symbol": "art-gallery",
                "title": "Seattle Art Museum",
                "description": "Premier art museum featuring contemporary and historic works"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3414, 47.6080]
              },
              "properties": {
                "name": "Olympic Sculpture Park",
                "type": "park",
                "size": "9 acres",
                "marker-color": "#2ecc71",
                "marker-size": "medium",
                "marker-symbol": "park",
                "title": "Olympic Sculpture Park",
                "description": "Free outdoor art museum with large-scale sculptures and waterfront views"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3520, 47.6147]
              },
              "properties": {
                "name": "Food Truck - Tacos El Tajin",
                "type": "food",
                "marker-color": "#f39c12",
                "marker-size": "small",
                "marker-symbol": "restaurant",
                "title": "Tacos El Tajin Food Truck",
                "description": "Popular Mexican food truck with authentic street tacos"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3489, 47.6156]
              },
              "properties": {
                "name": "Food Truck - Tat's Truck",
                "type": "food",
                "marker-color": "#f39c12",
                "marker-size": "small",
                "marker-symbol": "restaurant",
                "title": "Tat's Truck",
                "description": "Philadelphia-style sandwiches and East Coast cuisine"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3465, 47.6134]
              },
              "properties": {
                "name": "Food Truck - Falafel Salam",
                "type": "food",
                "marker-color": "#f39c12",
                "marker-size": "small",
                "marker-symbol": "restaurant",
                "title": "Falafel Salam",
                "description": "Middle Eastern cuisine featuring fresh falafel and shawarma"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3445, 47.6112]
              },
              "properties": {
                "name": "Food Truck - Where Ya At Matt",
                "type": "food",
                "marker-color": "#f39c12",
                "marker-size": "small",
                "marker-symbol": "restaurant",
                "title": "Where Ya At Matt",
                "description": "New Orleans style cuisine with po'boys and beignets"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3370, 47.6218]
              },
              "properties": {
                "name": "Kerry Park",
                "type": "viewpoint",
                "elevation": "456 ft",
                "marker-color": "#9b59b6",
                "marker-size": "large",
                "marker-symbol": "camera",
                "title": "Kerry Park Viewpoint",
                "description": "Best panoramic view of Seattle skyline, Space Needle, and Mount Rainier"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3480, 47.6205]
              },
              "properties": {
                "name": "Museum of Pop Culture",
                "type": "museum",
                "marker-color": "#e74c3c",
                "marker-size": "medium",
                "marker-symbol": "music",
                "title": "Museum of Pop Culture (MoPOP)",
                "description": "Contemporary pop culture museum in a distinctive Frank Gehry-designed building"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3394, 47.6186]
              },
              "properties": {
                "name": "Chihuly Garden and Glass",
                "type": "museum",
                "marker-color": "#3498db",
                "marker-size": "medium",
                "marker-symbol": "art-gallery",
                "title": "Chihuly Garden and Glass",
                "description": "Exhibition showcasing the studio glass of Dale Chihuly"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [-122.3537, 47.6219]
              },
              "properties": {
                "name": "Seattle Center Armory",
                "type": "food_hall",
                "marker-color": "#d35400",
                "marker-size": "medium",
                "marker-symbol": "restaurant",
                "title": "Seattle Center Armory",
                "description": "Food hall and event space with diverse dining options"
              }
            }
          ]
        }
        """;
        
    // Polygon layer: Seattle neighborhoods with advanced styling
    private readonly string _polygonsJSON =
        """
        {
          "type": "FeatureCollection",
          "style": {
            "title": "Seattle Neighborhoods",
            "description": "Key neighborhoods in Seattle with distinctive characteristics"
          },
          "features": [
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[
                  [-122.3567, 47.6062],
                  [-122.3567, 47.6162],
                  [-122.3367, 47.6162],
                  [-122.3367, 47.6062],
                  [-122.3567, 47.6062]
                ]]
              },
              "properties": {
                "name": "Seattle Center",
                "type": "district",
                "area": "74 acres",
                "attractions": "Space Needle, MoPOP, Chihuly Garden",
                "fill": "#4ecdc4",
                "fill-opacity": 0.3,
                "stroke": "#26a69a",
                "stroke-width": 3,
                "stroke-opacity": 0.9,
                "stroke-dasharray": "5,2",
                "title": "Seattle Center District",
                "description": "Cultural and entertainment district home to multiple world-class attractions"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[
                  [-122.350, 47.610],
                  [-122.345, 47.608],
                  [-122.340, 47.609],
                  [-122.336, 47.612],
                  [-122.341, 47.616],
                  [-122.350, 47.610]
                ]]
              },
              "properties": {
                "name": "Belltown",
                "type": "neighborhood",
                "population": "approximately 10,000",
                "fill": "#a8e6cf",
                "fill-opacity": 0.25,
                "stroke": "#7fcdcd",
                "stroke-width": 2,
                "stroke-dasharray": "4,1",
                "title": "Belltown Neighborhood",
                "description": "Dense urban neighborhood with high-rise condos, restaurants, and nightlife"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[
                  [-122.345, 47.600],
                  [-122.335, 47.600],
                  [-122.330, 47.605],
                  [-122.340, 47.608],
                  [-122.345, 47.600]
                ]]
              },
              "properties": {
                "name": "Downtown Seattle",
                "type": "downtown",
                "businesses": "over 5,000",
                "fill": "#ffd166",
                "fill-opacity": 0.35,
                "stroke": "#ef8d32",
                "stroke-width": 2.5,
                "stroke-opacity": 1.0,
                "title": "Downtown Seattle",
                "description": "Commercial heart of the city with major shopping, business headquarters, and civic institutions"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[
                  [-122.355, 47.625],
                  [-122.345, 47.625],
                  [-122.343, 47.620],
                  [-122.340, 47.618],
                  [-122.335, 47.620],
                  [-122.330, 47.617],
                  [-122.330, 47.628],
                  [-122.355, 47.628],
                  [-122.355, 47.625]
                ]]
              },
              "properties": {
                "name": "Queen Anne",
                "type": "residential",
                "elevation": "456 feet",
                "fill": "#9d8189",
                "fill-opacity": 0.2,
                "stroke": "#774c60",
                "stroke-width": 3,
                "stroke-dasharray": "8,3,2,3",
                "stroke-linecap": "round",
                "title": "Queen Anne",
                "description": "Historic residential neighborhood on a hill with stunning city views from Kerry Park"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[
                  [-122.335, 47.605],
                  [-122.330, 47.605],
                  [-122.325, 47.615],
                  [-122.330, 47.617],
                  [-122.335, 47.620],
                  [-122.340, 47.618],
                  [-122.335, 47.605]
                ]]
              },
              "properties": {
                "name": "South Lake Union",
                "type": "tech-hub",
                "companies": "Amazon, Facebook, Google offices",
                "fill": "#a1c4fd",
                "fill-opacity": 0.4,
                "stroke": "#8395e7",
                "stroke-width": 2,
                "stroke-opacity": 0.8,
                "title": "South Lake Union",
                "description": "Modern tech hub and home to Amazon headquarters with new apartment buildings and restaurants"
              }
            }
          ]
        }
        """;
        
    // LineString layer: Transit routes with advanced styling
    private readonly string _linesJSON =
        """
        {
          "type": "FeatureCollection",
          "style": {
            "title": "Seattle Transportation Routes",
            "description": "Key transportation routes in the Seattle area"
          },
          "features": [
            {
              "type": "Feature",
              "geometry": {
                "type": "LineString",
                "coordinates": [
                  [-122.3493, 47.6205],
                  [-122.3465, 47.6134],
                  [-122.3421, 47.6089],
                  [-122.3375, 47.6034],
                  [-122.3331, 47.5979]
                ]
              },
              "properties": {
                "name": "Tourist Walking Route",
                "type": "pedestrian",
                "distance": "2.1 miles",
                "duration": "45 minutes",
                "stroke": "#e63946",
                "stroke-width": 4,
                "stroke-opacity": 0.8,
                "stroke-dasharray": "8,3",
                "stroke-linecap": "round",
                "title": "Scenic Walking Tour",
                "description": "A pleasant walking route connecting major Seattle attractions from Space Needle to Art Museum"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "LineString",
                "coordinates": [
                  [-122.360, 47.610],
                  [-122.350, 47.610],
                  [-122.345, 47.608],
                  [-122.340, 47.609],
                  [-122.335, 47.605],
                  [-122.330, 47.605],
                  [-122.320, 47.600]
                ]
              },
              "properties": {
                "name": "South Lake Union Streetcar",
                "type": "transit",
                "length": "1.3 miles",
                "stops": "11 stations",
                "stroke": "#00b4d8",
                "stroke-width": 5,
                "stroke-opacity": 0.9,
                "stroke-dasharray": "10,2",
                "title": "South Lake Union Streetcar",
                "description": "Modern streetcar connecting Downtown to South Lake Union tech hub"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "LineString",
                "coordinates": [
                  [-122.320, 47.620],
                  [-122.330, 47.617],
                  [-122.335, 47.605],
                  [-122.345, 47.600],
                  [-122.348, 47.590],
                  [-122.350, 47.580],
                  [-122.355, 47.570]
                ]
              },
              "properties": {
                "name": "Link Light Rail",
                "type": "transit",
                "length": "22 miles",
                "stations": "16 stations",
                "stroke": "#4cc9f0",
                "stroke-width": 6,
                "stroke-opacity": 0.8,
                "title": "Link Light Rail",
                "description": "Seattle's modern light rail system connecting downtown with neighborhoods and the airport"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "LineString",
                "coordinates": [
                  [-122.370, 47.625],
                  [-122.355, 47.625],
                  [-122.345, 47.625],
                  [-122.335, 47.620],
                  [-122.330, 47.617],
                  [-122.325, 47.615]
                ]
              },
              "properties": {
                "name": "Metro Bus Route 3",
                "type": "bus",
                "frequency": "Every 15 minutes",
                "stroke": "#588157",
                "stroke-width": 3,
                "stroke-opacity": 0.8,
                "stroke-dasharray": "4,2,1,2",
                "title": "Metro Bus Route 3",
                "description": "Frequent bus route connecting Queen Anne to Downtown and Seattle Center"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "LineString",
                "coordinates": [
                  [-122.345, 47.625],
                  [-122.345, 47.615],
                  [-122.343, 47.608],
                  [-122.340, 47.600],
                  [-122.342, 47.592]
                ]
              },
              "properties": {
                "name": "Bike Path",
                "type": "cycling",
                "surface": "Dedicated protected lane",
                "stroke": "#56ab91",
                "stroke-width": 2,
                "stroke-opacity": 1.0,
                "stroke-dasharray": "2,1",
                "title": "Queen Anne Bike Path",
                "description": "Dedicated cycling infrastructure connecting Queen Anne hill to Downtown"
              }
            }
          ]
        }
        """;
    
    // MultiPoint layer: Public services with distinctive styles
    private readonly string _multipointsJSON =
        """
        {
          "type": "FeatureCollection",
          "style": {
            "title": "Seattle Public Services",
            "description": "Public service locations throughout Seattle"
          },
          "features": [
            {
              "type": "Feature",
              "geometry": {
                "type": "MultiPoint",
                "coordinates": [
                  [-122.3358, 47.6210],
                  [-122.3489, 47.6183],
                  [-122.3287, 47.6091],
                  [-122.3490, 47.6070],
                  [-122.3390, 47.6014]
                ]
              },
              "properties": {
                "name": "Public Libraries",
                "type": "education",
                "services": "Books, Internet, Community Programs",
                "marker-color": "#3f51b5",
                "marker-size": "medium",
                "marker-symbol": "library",
                "title": "Seattle Public Libraries",
                "description": "Branch locations of the Seattle Public Library system offering books and community services"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "MultiPoint",
                "coordinates": [
                  [-122.3328, 47.6154],
                  [-122.3418, 47.6094],
                  [-122.3520, 47.6200],
                  [-122.3410, 47.6004],
                  [-122.3505, 47.6120]
                ]
              },
              "properties": {
                "name": "Public WiFi Hotspots",
                "type": "technology",
                "access": "Free to public",
                "marker-color": "#4caf50",
                "marker-size": "small",
                "marker-symbol": "wifi",
                "title": "Free Public WiFi",
                "description": "Free public WiFi access points throughout downtown Seattle"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "MultiPoint",
                "coordinates": [
                  [-122.3480, 47.6159],
                  [-122.3380, 47.6091],
                  [-122.3432, 47.6046],
                  [-122.3509, 47.6105],
                  [-122.3396, 47.6207]
                ]
              },
              "properties": {
                "name": "Electric Vehicle Charging Stations",
                "type": "infrastructure",
                "chargers": "Level 2 and DC Fast Charging",
                "marker-color": "#673ab7",
                "marker-size": "medium",
                "marker-symbol": "charging-station",
                "title": "EV Charging Stations",
                "description": "Public electric vehicle charging locations throughout downtown Seattle and surrounding areas"
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "MultiPoint",
                "coordinates": [
                  [-122.3522, 47.6183],
                  [-122.3377, 47.6110],
                  [-122.3452, 47.6073],
                  [-122.3402, 47.6031],
                  [-122.3365, 47.6150]
                ]
              },
              "properties": {
                "name": "Public Art Installations",
                "type": "culture",
                "installations": "Sculptures, Murals, Interactive Art",
                "marker-color": "#ff4081",
                "marker-size": "small",
                "marker-symbol": "monument",
                "title": "Public Art Locations",
                "description": "Public art installations located throughout downtown Seattle and Seattle Center"
              }
            }
          ]
        }
        """;
}