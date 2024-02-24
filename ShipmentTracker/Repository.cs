using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace ShipmentTracker;

public class Repository
{
    public List<Shipment> GetShipments()
    {
        string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "export.json");
        string json = File.ReadAllText(jsonPath);
        List<Shipment> shipments = JsonSerializer.Deserialize<List<Shipment>>(json,
            new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            })!;

        return shipments.Take(100).ToList();
    }

    private readonly Random _random = new();
}