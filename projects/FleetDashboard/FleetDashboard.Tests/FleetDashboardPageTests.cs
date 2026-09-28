using Bunit;
using Bunit.TestDoubles;
using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Core.Components.Views;
using FleetDashboard.Client.Components;
using FleetDashboard.Client.Pages;
using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetDashboardPageTests
{
    [TestMethod]
    public async Task DashboardStartsWithMapStatusAndSharedSelection()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Initializing map");
        StringAssert.Contains(page.Markup, "Simulated telemetry");
        StringAssert.Contains(page.Markup, "Choose a truck");
        page.Find("button[data-vehicle-id='7']").Click();

        Assert.AreEqual(7, page.FindComponent<Stub<FleetMap>>().Instance.Parameters.Get(p => p.SelectedVehicleId));
        StringAssert.Contains(page.Find(".selected-details").TextContent, "Truck 007");
    }

    [TestMethod]
    public async Task TheModeQuerySelectsTheDemoPipelineAndPreset()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/?mode=baseline");
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Mode: baseline");
        StringAssert.Contains(page.Markup, "Load: stress");
    }

    [TestMethod]
    public async Task APresetQueryOverridesTheModeDefaultWorkload()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/?mode=latest&preset=stress");
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Mode: latest");
        StringAssert.Contains(page.Markup, "Load: stress");
    }

    [TestMethod]
    public async Task AnUnknownPresetFallsBackToTheModeDefaultWithAVisibleNotice()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/?mode=latest&preset=turbo");
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Load: normal");
        StringAssert.Contains(page.Markup, "Unknown preset 'turbo'");
    }

    [TestMethod]
    public async Task AnUnknownModeFallsBackToReadyWithAVisibleNotice()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/?mode=turbo");
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Mode: ready");
        StringAssert.Contains(page.Markup, "Load: normal");
        StringAssert.Contains(page.Markup, "Unknown mode 'turbo'");
    }

    [TestMethod]
    public async Task AFailedInitialConnectionShowsARetryActionThatRecovers()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new RecoverableSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Telemetry unavailable");
        var retry = page.FindAll("button").First(button => button.TextContent.Contains("Retry connection"));
        retry.Click();

        page.WaitForAssertion(() => Assert.IsFalse(page.Markup.Contains("Telemetry unavailable")));
    }

    private sealed class RecoverableSource : IFleetFrameSource, IFleetConnectionStatus
    {
        public bool IsReconnecting => false;

        public int ReconnectCount => 0;

        public Task RetryConnectionAsync(CancellationToken cancellationToken = default)
        {
            _allowed = true;
            return Task.CompletedTask;
        }

        public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset instant,
            CancellationToken cancellationToken = default)
        {
            if (!_allowed) throw new InvalidOperationException("The fleet service is unavailable.");
            return ValueTask.FromResult(new FleetFrame(Guid.Empty, ++_sequence, instant,
                [new VehicleReport(7, _sequence, instant, -75.34, 41.09, 42, "swiftwater")]));
        }

        private bool _allowed;
        private long _sequence;
    }

    private sealed class FakeSource : IFleetFrameSource
    {
        public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset instant, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new FleetFrame(Guid.Empty, ++_sequence, instant,
                [new VehicleReport(7, _sequence, instant, -75.34, 41.09, 42, "swiftwater")]));
        }

        private long _sequence;
    }

    private sealed class QuietMapView : MapView
    {
        public override Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}
