namespace CustomPopups.Shared;

public class DataSet
{
    public double? Latitude;
    public double? Longitude;
    public string? Name;
    public string? Description;

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
                Description = $"This is a description {i}"
            };
            data.Add(r);
        }

        return data;
    }
}
