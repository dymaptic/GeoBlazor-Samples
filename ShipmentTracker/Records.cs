using dymaptic.GeoBlazor.Core.Objects;

namespace ShipmentTracker;

public record Shipment(
    int Id,
    string Category,
    string SubCategory,
    string Name,
    double Latitude,
    double Longitude,
    int Quantity,
    double Weight);

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