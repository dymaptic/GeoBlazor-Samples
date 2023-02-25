namespace CustomPopups.Shared;

public class DataSet
{
    public double? Latitude;
    public double? Longitude;
    public string? Name;
    public string? Description;
    public string? Address;

    public Dictionary<string, object> Attributes =>
        new Dictionary<string, object>()
        {
            {nameof(Name), Name},
            {nameof(Description), Description},
            {nameof(Address), Address}
        };

    public static List<DataSet> GenerateSomePoints(double centerLat, double centerLong)
    {
        var data = new List<DataSet>();
        for (int i = 0; i < 10; i++)
        {
            var r = new DataSet()
            {
                Latitude = centerLat + Random.Shared.NextDouble(),
                Longitude = centerLong + Random.Shared.NextDouble(),
                Name = $"This is a Name {i}",
                Description = $"This is a description {i}",
                Address = $"{i}{i}{i} Some Road Name"
            };
            data.Add(r);
        }

        return data;
    }
}
