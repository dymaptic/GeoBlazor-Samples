using FleetDashboard.Client;
using FleetDashboard.Client.Streaming;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetServiceRegistrationTests
{
    private static readonly Uri BaseAddress = new("https://example.test/");

    [TestMethod]
    [DataRow("Browser")]
    [DataRow("browser")]
    [DataRow("BROWSER")]
    public void TheBrowserSourceReplacesTheHubConnection(string value)
    {
        var services = new ServiceCollection();

        services.AddFleetFrameSource(Configure(("Fleet:Source", value)), BaseAddress);

        Assert.AreEqual(typeof(BrowserFleetFrameSource), FrameSourceType(services));
        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(HubConnection)));
        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(IFleetHubConnection)));
    }

    [TestMethod]
    public void WithoutTheSettingTheHubSourceIsRegistered()
    {
        var services = new ServiceCollection();

        services.AddFleetFrameSource(Configure(), BaseAddress);

        Assert.AreEqual(typeof(SignalRFleetFrameSource), FrameSourceType(services));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(HubConnection)));
        Assert.IsTrue(services.Any(d => d.ServiceType == typeof(IFleetHubConnection)));
    }

    [TestMethod]
    [DataRow("SignalR")]
    [DataRow("")]
    [DataRow("Server")]
    public void AnyOtherValueKeepsTheHubSource(string value)
    {
        var services = new ServiceCollection();

        services.AddFleetFrameSource(Configure(("Fleet:Source", value)), BaseAddress);

        Assert.AreEqual(typeof(SignalRFleetFrameSource), FrameSourceType(services));
    }

    private static Type? FrameSourceType(IServiceCollection services) =>
        services.Single(d => d.ServiceType == typeof(IFleetFrameSource)).ImplementationType;

    private static IConfiguration Configure(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();
}
