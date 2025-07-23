using System.Text.Json;
using Microsoft.Extensions.FileProviders;

namespace ShipmentTracker;

public class Repository
{
    public List<Shipment> GetShipments()
    {
        string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "export.json");
        string json = File.ReadAllText(jsonPath);
        var basicShipments = JsonSerializer.Deserialize<List<BasicShipment>>(json,
            new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            })!;

        // Enhance shipments with additional data
        return basicShipments.Take(100).Select((s, index) => new Shipment(
            s.Id,
            s.Category,
            s.SubCategory,
            s.Name,
            s.Latitude,
            s.Longitude,
            s.Quantity,
            s.Weight,
            GenerateTrackingNumber(s.Id),
            GenerateCustomerName(),
            GenerateCustomerEmail(),
            "1234 Warehouse Dr, Chicago, IL 60601",
            GenerateDestinationAddress(),
            GenerateStatus(),
            DateTime.Now.AddDays(-_random.Next(1, 30)),
            GenerateDeliveredDate()
        )).ToList();
    }

    public Shipment? GetShipmentByTrackingNumber(string trackingNumber)
    {
        return GetShipments().FirstOrDefault(s => s.TrackingNumber.Equals(trackingNumber, StringComparison.OrdinalIgnoreCase));
    }

    public List<Shipment> SearchShipments(SearchCriteria criteria)
    {
        var shipments = GetShipments();

        if (!string.IsNullOrWhiteSpace(criteria.TrackingNumber))
        {
            shipments = shipments.Where(s => s.TrackingNumber.Contains(criteria.TrackingNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(criteria.CustomerName))
        {
            shipments = shipments.Where(s => s.CustomerName.Contains(criteria.CustomerName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (criteria.StartDate.HasValue)
        {
            shipments = shipments.Where(s => DateOnly.FromDateTime(s.CreatedDate) >= criteria.StartDate.Value).ToList();
        }

        if (criteria.EndDate.HasValue)
        {
            shipments = shipments.Where(s => DateOnly.FromDateTime(s.CreatedDate) <= criteria.EndDate.Value).ToList();
        }

        if (criteria.Status.HasValue)
        {
            shipments = shipments.Where(s => s.Status == criteria.Status.Value).ToList();
        }

        return shipments;
    }

    private string GenerateTrackingNumber(int id) => $"LT{DateTime.Now.Year}{id:D6}";
    
    private string GenerateCustomerName()
    {
        var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Emma", "Robert", "Lisa" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        return $"{firstNames[_random.Next(firstNames.Length)]} {lastNames[_random.Next(lastNames.Length)]}";
    }

    private string GenerateCustomerEmail()
    {
        var domains = new[] { "gmail.com", "yahoo.com", "outlook.com", "company.com" };
        return $"customer{_random.Next(1000, 9999)}@{domains[_random.Next(domains.Length)]}";
    }

    private string GenerateDestinationAddress()
    {
        var streets = new[] { "Main St", "Oak Ave", "Elm Dr", "Park Blvd", "First St" };
        var cities = new[] { "New York, NY", "Los Angeles, CA", "Houston, TX", "Phoenix, AZ", "Philadelphia, PA" };
        return $"{_random.Next(100, 9999)} {streets[_random.Next(streets.Length)]}, {cities[_random.Next(cities.Length)]} {_random.Next(10000, 99999)}";
    }

    private ShipmentStatus GenerateStatus()
    {
        var rand = _random.Next(100);
        if (rand < 60) return ShipmentStatus.InTransit;
        if (rand < 80) return ShipmentStatus.Delivered;
        if (rand < 95) return ShipmentStatus.Pending;
        return ShipmentStatus.Delayed;
    }

    private DateTime? GenerateDeliveredDate()
    {
        return _random.Next(100) < 20 ? DateTime.Now.AddDays(-_random.Next(1, 10)) : null;
    }

    private record BasicShipment(
        int Id,
        string Category,
        string SubCategory,
        string Name,
        double Latitude,
        double Longitude,
        int Quantity,
        double Weight
    );

    private readonly Random _random = new();
}