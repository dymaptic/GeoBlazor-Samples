using Bunit;
using FleetDashboard.Client.Components;
using FleetDashboard.Client.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class DashboardComponentTests
{
    [TestMethod]
    public void ChartOmitsExpiredSamples()
    {
        using var context = new BunitContext();
        DateTimeOffset instant = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        SpeedSample[] samples = [new(instant, 20), new(instant.AddSeconds(1), 30),
            new(instant.AddSeconds(64), 40), new(instant.AddSeconds(65), 42)];
        var component = context.Render<SpeedChart>(parameters => parameters
            .Add(p => p.Samples, samples).Add(p => p.WindowEndUtc, instant.AddSeconds(65)));

        Assert.HasCount(1, component.FindAll("path.speed-segment"));
    }

    [TestMethod]
    public void SpeedChartSplitsMissingIntervals()
    {
        using var context = new BunitContext();
        DateTimeOffset instant = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        SpeedSample[] samples = [new(instant, 20), new(instant.AddSeconds(1), 30),
            new(instant.AddSeconds(4), 40), new(instant.AddSeconds(5), 42)];
        var component = context.Render<SpeedChart>(parameters => parameters
            .Add(p => p.Samples, samples).Add(p => p.LatestSpeedKph, 42));

        Assert.AreEqual("Sampled speed (km/h), last 60 seconds", component.Find("svg").GetAttribute("aria-label"));
        StringAssert.Contains(component.Markup, "42 km/h");
        Assert.HasCount(2, component.FindAll("path.speed-segment"));
    }

    [TestMethod]
    public void VehicleButtonSelectsTruckSeven()
    {
        using var context = new BunitContext();
        int? selected = null;
        VehicleListItem[] vehicles = [Vehicle(2), Vehicle(7), Vehicle(18)];
        var component = context.Render<VehicleList>(parameters => parameters
            .Add(p => p.Vehicles, vehicles)
            .Add(p => p.SelectedVehicleId, 7)
            .Add(p => p.SelectedVehicleIdChanged, id => selected = id));

        component.Find("button[data-vehicle-id='7']").Click();

        Assert.AreEqual(7, selected);
        Assert.AreEqual("true", component.Find("button[data-vehicle-id='7']").GetAttribute("aria-pressed"));
        Assert.HasCount(3, component.FindAll("button[data-vehicle-id]"));
    }

    private static VehicleListItem Vehicle(int id)
    {
        return new VehicleListItem(id, "swiftwater", "Swiftwater loop", -75.34, 41.09, 42,
            DateTimeOffset.Parse("2026-09-08T12:00:00Z"), VehicleStatus.Moving, 0);
    }
}
