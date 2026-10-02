using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;

namespace SpatialDispatch.Tests;

public class DemoContractTests
{
    [Fact]
    public void Company_extent_contains_every_seeded_location()
    {
        IEnumerable<GeoPoint> locations = DemoContract.Jobs.Select(job => job.Location)
            .Concat(DemoContract.Technicians.Select(technician => technician.Location));

        Assert.All(locations, location =>
        {
            Assert.InRange(
                location.Longitude,
                DemoContract.CompanyExtentWest,
                DemoContract.CompanyExtentEast);
            Assert.InRange(
                location.Latitude,
                DemoContract.CompanyExtentSouth,
                DemoContract.CompanyExtentNorth);
        });
    }

    [Fact]
    public void Company_extent_diagonal_covers_every_recorded_distance()
    {
        IEnumerable<double> distances = DemoContract.ReferenceDistanceMeters.Values
            .SelectMany(distanceByTechnician => distanceByTechnician.Values);

        Assert.All(
            distances,
            distance => Assert.InRange(distance, 0d, DemoContract.CompanyExtentDiagonalMeters));
    }
}
