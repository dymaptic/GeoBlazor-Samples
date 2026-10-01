using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Converters;
using SpatialDispatch.Data;
using SpatialDispatch.Data.Entities;
using SpatialDispatch.Shared.Demo;

namespace SpatialDispatch.Tests;

/// <summary>
///     Checks the GeoJSON the map's layers consume: coordinate order, property names, and value types.
/// </summary>
/// <remarks>
///     No database is involved. The layer shape is a contract between the API and
///     <c>DispatchMap.razor</c>, whose highlight layer filters on <c>TerritoryId</c>, and that contract can
///     break from a rename alone. The static files under <c>wwwroot/data</c> are the other side of it: the
///     earlier checkpoint branches serve those instead of the API, so the two must agree.
/// </remarks>
public class MapLayerFeaturesTests
{
    private static readonly JsonSerializerOptions GeoJson = CreateOptions();

    [Fact]
    public void Territory_layer_carries_the_attribute_the_map_filters_on()
    {
        JsonElement collection = Serialize(MapLayerFeatures.ForTerritories(Territories()));
        JsonElement[] features = collection.GetProperty("features").EnumerateArray().ToArray();

        Assert.Equal(DemoContract.Territories.Count, features.Length);

        foreach (JsonElement feature in features)
        {
            JsonElement properties = feature.GetProperty("properties");

            // DispatchMap.razor sets DefinitionExpression="TerritoryId = n". Rename this and the territory
            // silently stops highlighting, with no error anywhere.
            Assert.Equal(JsonValueKind.Number, properties.GetProperty("TerritoryId").ValueKind);
            Assert.Equal(JsonValueKind.String, properties.GetProperty("Name").ValueKind);
        }
    }

    [Fact]
    public void Territory_coordinates_are_written_longitude_first()
    {
        JsonElement collection = Serialize(MapLayerFeatures.ForTerritories(Territories()));

        JsonElement firstVertex = collection
            .GetProperty("features")[0]
            .GetProperty("geometry")
            .GetProperty("coordinates")[0][0];

        // RFC 7946 puts longitude before latitude, and so does every coordinate in the demo contract. A
        // reversed pair here would still be valid GeoJSON, which is exactly why it needs asserting.
        Assert.Equal(-75.830000, firstVertex[0].GetDouble(), 6);
        Assert.Equal(41.352000, firstVertex[1].GetDouble(), 6);
    }

    [Fact]
    public void Technician_coordinates_are_written_longitude_first()
    {
        JsonElement collection = Serialize(MapLayerFeatures.ForTechnicians(Technicians()));

        JsonElement point = collection
            .GetProperty("features")[0]
            .GetProperty("geometry")
            .GetProperty("coordinates");

        Assert.Equal(-75.680500, point[0].GetDouble(), 6);
        Assert.Equal(41.361200, point[1].GetDouble(), 6);
    }

    [Fact]
    public void Technician_availability_is_written_as_a_number_not_a_boolean()
    {
        JsonElement collection = Serialize(MapLayerFeatures.ForTechnicians(Technicians()));
        JsonElement[] features = collection.GetProperty("features").EnumerateArray().ToArray();

        Assert.Equal(DemoContract.Technicians.Count, features.Length);

        foreach (JsonElement feature in features)
        {
            JsonElement available = feature.GetProperty("properties").GetProperty("IsAvailable");

            Assert.Equal(JsonValueKind.Number, available.ValueKind);
            Assert.Contains(available.GetInt32(), new[] { 0, 1 });
        }
    }

    [Fact]
    public void Layer_properties_match_the_static_files_the_dashboard_shipped_with()
    {
        AssertSamePropertyShape(
            Path.Combine("data", "territories.geojson"),
            MapLayerFeatures.ForTerritories(Territories()));

        AssertSamePropertyShape(
            Path.Combine("data", "technicians.geojson"),
            MapLayerFeatures.ForTechnicians(Technicians()));
    }

    private static void AssertSamePropertyShape(string staticFilePath, FeatureCollection generated)
    {
        using JsonDocument expected = JsonDocument.Parse(File.ReadAllText(staticFilePath));

        JsonElement actualFeatures = Serialize(generated).GetProperty("features");
        JsonElement expectedFeatures = expected.RootElement.GetProperty("features");

        Assert.Equal(expectedFeatures.GetArrayLength(), actualFeatures.GetArrayLength());

        // Comparing the identifiers as well as the count is what actually catches drift: a technician added
        // to DemoContract but not to the static file, or the same nine features in a different order.
        Assert.Equal(FeatureIds(expectedFeatures), FeatureIds(actualFeatures));

        for (int index = 0; index < expectedFeatures.GetArrayLength(); index++)
        {
            Dictionary<string, JsonValueKind> expectedShape = PropertyShape(expectedFeatures[index]);
            Dictionary<string, JsonValueKind> actualShape = PropertyShape(actualFeatures[index]);

            Assert.Equal(expectedShape, actualShape);
        }
    }

    /// <summary>
    ///     Every feature's own identifier, in file order. Territories carry <c>TerritoryId</c> and
    ///     technicians carry <c>TechnicianId</c>, so this takes whichever the collection uses.
    /// </summary>
    private static List<int> FeatureIds(JsonElement features) =>
        features.EnumerateArray()
            .Select(feature => feature.GetProperty("properties"))
            .Select(properties =>
                properties.TryGetProperty("TechnicianId", out JsonElement technicianId)
                    ? technicianId.GetInt32()
                    : properties.GetProperty("TerritoryId").GetInt32())
            .ToList();

    private static Dictionary<string, JsonValueKind> PropertyShape(JsonElement feature) =>
        feature.GetProperty("properties")
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ValueKind);

    private static JsonElement Serialize(FeatureCollection collection) =>
        JsonSerializer.SerializeToElement(collection, GeoJson);

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new GeoJsonConverterFactory());

        return options;
    }

    private static List<Territory> Territories() =>
        DemoContract.Territories
            .Select(t => new Territory
            {
                Id = t.Id,
                Name = t.Name,
                Boundary = DemoDataSeeder.BoundaryFor(t.Id)
            })
            .ToList();

    private static List<Technician> Technicians() =>
        DemoContract.Technicians
            .Select(t => new Technician
            {
                Id = t.Id,
                Name = t.Name,
                TerritoryId = t.TerritoryId,
                Territory = new Territory
                {
                    Id = t.TerritoryId,
                    Name = DemoContract.TerritoryById(t.TerritoryId).Name,
                    Boundary = DemoDataSeeder.BoundaryFor(t.TerritoryId)
                },
                IsAvailable = t.IsAvailable,
                LocationLabel = t.LocationLabel,
                Location = DemoDataSeeder.ToPoint(t.Location)
            })
            .ToList();
}
