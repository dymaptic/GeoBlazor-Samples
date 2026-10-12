using Bunit;
using Bunit.TestDoubles;
using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Core.Components.Views;
using FleetDashboard.Client.Components;
using FleetDashboard.Client.Pages;
using FleetDashboard.Client.Rendering;
using FleetDashboard.Client.Streaming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FleetDashboard.Tests;

[TestClass]
public sealed class LiveLayerTests
{
    [TestMethod]
    public void TrafficIsOfferedOnlyWhenAnApiKeyIsConfigured()
    {
        CollectionAssert.AreEqual(new[] { "radar", "warnings" },
            HostedLayers.Available(hasApiKey: false).Select(l => l.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "radar", "warnings", "traffic" },
            HostedLayers.Available(hasApiKey: true).Select(l => l.Id).ToArray());
    }

    [TestMethod]
    public void ABlankKeyCountsAsNoKey()
    {
        Assert.IsFalse(HostedLayers.HasApiKey(Config("")));
        Assert.IsFalse(HostedLayers.HasApiKey(Config("  ")));
        Assert.IsTrue(HostedLayers.HasApiKey(Config("abc")));
    }

    [TestMethod]
    public async Task HomeShowsNoTrafficToggleWithoutAKeyAndAllLayersStartOff()
    {
        await using var context = CreateContext(Config(null));
        var page = context.Render<Home>();

        Assert.AreEqual(0, page.FindAll("[data-layer-id='traffic']").Count);
        Assert.AreEqual(2, page.FindAll(".live-layers button").Count);
        Assert.IsTrue(page.FindAll(".live-layers button").All(b => b.GetAttribute("aria-pressed") == "false"));
    }

    [TestMethod]
    public async Task HomeShowsTheTrafficToggleWithAKeyAndTogglesPressedState()
    {
        await using var context = CreateContext(Config("abc"));
        var page = context.Render<Home>();

        Assert.AreEqual(3, page.FindAll(".live-layers button").Count);
        page.Find("[data-layer-id='traffic']").Click();
        Assert.AreEqual("true", page.Find("[data-layer-id='traffic']").GetAttribute("aria-pressed"));
        Assert.AreEqual("false", page.Find("[data-layer-id='radar']").GetAttribute("aria-pressed"));
        var visible = page.FindComponent<Stub<FleetMap>>().Instance.Parameters.Get(p => p.VisibleHostedLayers);
        CollectionAssert.AreEquivalent(new[] { "traffic" }, visible.ToArray());

        page.Find("[data-layer-id='traffic']").Click();
        Assert.AreEqual("false", page.Find("[data-layer-id='traffic']").GetAttribute("aria-pressed"));
    }

    private static IConfiguration Config(string? key) => new ConfigurationBuilder()
        .AddInMemoryCollection(key is null ? [] : [new(HostedLayers.ApiKeyConfigName, key)]).Build();

    private static BunitContext CreateContext(IConfiguration config)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FleetDashboardPageTests.FakeSource());
        context.ComponentFactories.Add<MapView>(() => new FleetDashboardPageTests.QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        return context;
    }
}
