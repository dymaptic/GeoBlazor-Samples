using dymaptic.GeoBlazor.Core.Model;

namespace ShipmentTracker;

public record Shipment(
    int Id,
    string Category,
    string SubCategory,
    string Name,
    double Latitude,
    double Longitude,
    int Quantity,
    double Weight,
    string TrackingNumber = "",
    string CustomerName = "",
    string CustomerEmail = "",
    string OriginAddress = "",
    string DestinationAddress = "",
    ShipmentStatus Status = ShipmentStatus.InTransit,
    DateTime CreatedDate = default,
    DateTime? DeliveredDate = null);

public enum ShipmentStatus { Pending, InTransit, Delivered, Delayed }

public record SearchCriteria(
    string? TrackingNumber,
    string? CustomerName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    ShipmentStatus? Status);

public record TrackingEvent(
    DateTime Timestamp,
    string Location,
    string Description,
    double? Latitude = null,
    double? Longitude = null);

public static class CategoryColors
{
    public static readonly Dictionary<string, MapColor> AllColors = new()
    {
        {"Other", Other},
        {"Beauty", Beauty},
        {"Electronics", Electronics},
        {"Entertainment", Entertainment},
        {"Home", Home},
        {"Activity", Activity},
        {"Health", Health},
        {"Pets", Pets},
        {"Clothing", Clothing},
        {"Automotive", Automotive}
    };
    
    public static MapColor Other => new("blue");
    public static MapColor Beauty => new("yellow");
    public static MapColor Electronics => new("green");
    public static MapColor Entertainment => new("red");
    public static MapColor Home => new("orange");
    public static MapColor Activity => new("purple");
    public static MapColor Health => new("brown");
    public static MapColor Pets => new("pink");
    public static MapColor Clothing => new("black");
    public static MapColor Automotive => new("lightgreen");
}