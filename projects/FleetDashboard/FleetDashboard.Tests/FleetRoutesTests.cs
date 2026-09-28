using FleetDashboard.Shared.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FleetDashboard.Tests;

[TestClass]
public sealed class FleetRoutesTests
{
    [TestMethod]
    public void PreparedFileSuppliesTheFourNamedRoutes()
    {
        string[] expected = ["swiftwater", "pocono", "tannersville", "manor"];
        CollectionAssert.AreEquivalent(expected, FleetRoutes.All.Select(route => route.Id).ToArray());

        foreach (FleetRoute route in FleetRoutes.All)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(route.Name), $"Route '{route.Id}' needs a name.");
            Assert.IsGreaterThanOrEqualTo(2, route.Points.Count, $"Route '{route.Id}' needs vertices.");
            Assert.IsGreaterThan(0, route.LengthKm, $"Route '{route.Id}' needs length.");
        }
    }

    [TestMethod]
    public void AdjacentVerticesAreConnectedAtRoadScale()
    {
        foreach (FleetRoute route in FleetRoutes.All)
        {
            for (int index = 0; index < route.Points.Count - 1; index++)
            {
                double segmentKm = DistanceKm(route.Points[index], route.Points[index + 1]);
                Assert.IsGreaterThan(0, segmentKm,
                    $"Route '{route.Id}' repeats vertex {index}; zero-length segments break distance math.");
                Assert.IsLessThanOrEqualTo(FleetRoutes.MaximumSegmentKm, segmentKm,
                    $"Route '{route.Id}' jumps {segmentKm:0.00} km between vertices {index} and {index + 1}.");
            }
        }
    }

    [TestMethod]
    public void GeometryIsDenseEnoughToFollowRoads()
    {
        // A schematic straight line has two to four vertices. Real road geometry has many, and
        // its average vertex spacing stays far below the straight-line span of the route.
        foreach (FleetRoute route in FleetRoutes.All)
        {
            Assert.IsGreaterThanOrEqualTo(20, route.Points.Count,
                $"Route '{route.Id}' looks schematic; expected road-following vertices.");
            double averageSegmentKm = route.LengthKm / (route.Points.Count - 1);
            Assert.IsLessThan(0.25, averageSegmentKm,
                $"Route '{route.Id}' averages {averageSegmentKm:0.00} km per vertex, too coarse for a road.");
        }
    }

    [TestMethod]
    public void RoutesStayInThePoconoRegion()
    {
        foreach (FleetRoute route in FleetRoutes.All)
        {
            foreach (RoutePoint point in route.Points)
            {
                Assert.IsTrue(point.Longitude is >= -75.5 and <= -75.1,
                    $"Route '{route.Id}' leaves the demo region at longitude {point.Longitude}.");
                Assert.IsTrue(point.Latitude is >= 40.9 and <= 41.25,
                    $"Route '{route.Id}' leaves the demo region at latitude {point.Latitude}.");
            }
        }
    }

    [TestMethod]
    public void ARouteReversesAtItsFarEndWithoutAClosingSegment()
    {
        foreach (FleetRoute route in FleetRoutes.All)
        {
            RoutePoint before = route.AtDistance(route.LengthKm - 0.05);
            RoutePoint after = route.AtDistance(route.LengthKm + 0.05);
            Assert.AreEqual(before.Longitude, after.Longitude, 1e-9, route.Id);
            Assert.AreEqual(before.Latitude, after.Latitude, 1e-9, route.Id);
        }
    }

    private static double DistanceKm(RoutePoint start, RoutePoint end)
    {
        double latitudeRadians = (end.Latitude - start.Latitude) * Math.PI / 180;
        double longitudeRadians = (end.Longitude - start.Longitude) * Math.PI / 180;
        double haversine = Math.Pow(Math.Sin(latitudeRadians / 2), 2)
            + (Math.Cos(start.Latitude * Math.PI / 180) * Math.Cos(end.Latitude * Math.PI / 180)
                * Math.Pow(Math.Sin(longitudeRadians / 2), 2));
        return 12_742 * Math.Asin(Math.Sqrt(haversine));
    }
}
