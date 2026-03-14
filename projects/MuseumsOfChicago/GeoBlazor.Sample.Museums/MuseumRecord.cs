using CsvHelper.Configuration.Attributes;

namespace GeoBlazor.Sample.Museums;

public record MuseumRecord
{
    [Name("COMMONNAME")]
    public required string CommonName { get; init; }

    [Name("GSTREET")]
    public required string StreetAddress { get; init; }

    [Name("PHONE")]
    public required string PhoneNumber { get; init; }

    [Name("WEBURL")]
    public required string Website { get; init; }

    [Name("LATITUDE")]
    public required double Latitude { get; init; }

    [Name("LONGITUDE")]
    public required double Longitude { get; init; }

    [Name("DISCIPL")]
    public required string Discipline { get; init; }
}