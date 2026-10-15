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
        StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent, DemoMode.Baseline.WatchingLine());
        StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent, DemoMode.Baseline.DemonstrationCue());
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
    public async Task ViewportModeSendsTheSettledExtentAndTheSelection()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        var source = new ViewportSource();
        context.Services.AddSingleton<IFleetFrameSource>(source);
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        context.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/?mode=viewport&preset=stress");
        var page = context.Render<Home>();
        var extent = new ViewportExtent(-75.36, 41.07, -75.33, 41.09);

        page.FindComponent<Stub<FleetMap>>().Instance.Parameters.Get(p => p.ViewExtentChanged)!(extent);
        page.Find("button[data-vehicle-id='7']").Click();

        page.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new[] { extent }, source.Extents);
            Assert.AreEqual(7, source.Selections.Last());
        }, TimeSpan.FromSeconds(5));
        StringAssert.Contains(page.Markup, "Mode: viewport");
        StringAssert.Contains(page.Markup, "Load: stress");
        StringAssert.Contains(page.Markup, "Vehicles/frame: 1");
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
    public async Task TheModePickerExplainsTheSelectionBeforeApplyingIt()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var page = context.Render<Home>();

        CollectionAssert.AreEqual(new[]
        {
            "Finished dashboard · ready",
            "Raw queue · baseline",
            "Raw queue with 250 record cap per tick · pipeline",
            "Keep the latest per truck · latest",
            "Batched ApplyEdits calls · batched",
            "Batched ApplyEdits with 250 ms tick · scheduled",
            "Filter by view extent · viewport",
            "Failure scenarios · resilient"
        }, page.FindAll("#preview-mode option").Select(option => option.TextContent).ToArray());
        StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent,
            "Normal load: one report per truck, one ApplyEdits call every 250 ms. Same pipeline as scheduled and resilient.");

        page.Find("#preview-mode").Change("baseline");

        StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent, DemoMode.Baseline.WatchingLine());
        StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent, DemoMode.Baseline.DemonstrationCue());

        foreach (DemoMode mode in Enum.GetValues<DemoMode>())
        {
            page.Find("#preview-mode").Change(mode.Label());

            Assert.AreEqual(mode.Label(), page.Find("#preview-mode").GetAttribute("value"));
            StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent, mode.WatchingLine());
            StringAssert.Contains(page.Find("#preview-mode-guidance").TextContent, mode.DemonstrationCue());
        }

        Assert.AreEqual("http://localhost/", navigation.Uri);
        StringAssert.Contains(page.Markup, "Mode: ready");
        Assert.AreEqual("normal", page.Find("#preview-load").GetAttribute("value"));
    }

    [TestMethod]
    public async Task ThePreviewControlsReloadTheDashboardInTheChosenModeAndLoad()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new FakeSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var page = context.Render<Home>();

        Assert.AreEqual("ready", page.Find("select#preview-mode").GetAttribute("value"));
        Assert.AreEqual("normal", page.Find("select#preview-load").GetAttribute("value"));
        page.Find("select#preview-mode").Change("baseline");
        page.Find("select#preview-load").Change("ceiling");
        page.Find("button.apply-preview").Click();

        Assert.AreEqual("http://localhost/?mode=baseline&preset=ceiling", navigation.Uri);
    }

    [TestMethod]
    public async Task ApplyingThePreviewResetsTheServerRunBeforeReloading()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        var source = new ScenarioControlSource { AcceptsPreset = true };
        context.Services.AddSingleton<IFleetFrameSource>(source);
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var page = context.Render<Home>();

        page.Find("select#preview-mode").Change("latest");
        page.Find("select#preview-load").Change("stress");
        page.Find("button.apply-preview").Click();

        Assert.AreEqual(1, source.ResetCount);
        Assert.AreEqual("http://localhost/?mode=latest&preset=stress", navigation.Uri);
    }

    [TestMethod]
    public async Task ApplyingThePreviewStillReloadsWhenTheServerResetFails()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        var source = new ScenarioControlSource { AcceptsPreset = true, FailsReset = true };
        context.Services.AddSingleton<IFleetFrameSource>(source);
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        var page = context.Render<Home>();

        page.Find("select#preview-mode").Change("baseline");
        page.Find("button.apply-preview").Click();

        Assert.AreEqual(1, source.ResetCount);
        Assert.AreEqual("http://localhost/?mode=baseline&preset=normal", navigation.Uri);
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

    [TestMethod]
    public async Task ARequestedPresetStaysPendingAndReportsItsOwnErrorUntilTheHubAcceptsIt()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<IFleetFrameSource>(new ScenarioControlSource());
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/?mode=baseline");
        var page = context.Render<Home>();

        // The preset stays visibly pending and its failure never masquerades as a telemetry outage.
        StringAssert.Contains(page.Markup, "Load: stress (applying)");
        StringAssert.Contains(page.Markup, "Scenario command failed");
        Assert.IsFalse(page.Markup.Contains("Telemetry unavailable"));
    }

    [TestMethod]
    public async Task ThePresetRequestReachesTheHubOnceTheFrameSourceAcceptsCommands()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        var source = new ScenarioControlSource { AcceptsPreset = true };
        context.Services.AddSingleton<IFleetFrameSource>(source);
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        context.ComponentFactories.AddStub<FleetMap>();
        var page = context.Render<Home>();

        StringAssert.Contains(page.Markup, "Load: normal");
        Assert.IsFalse(page.Markup.Contains("(applying)"));
        Assert.IsFalse(page.Markup.Contains("Scenario command failed"));
        CollectionAssert.AreEqual(new[] { "normal" }, source.Presets.ToArray());
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

    internal sealed class FakeSource : IFleetFrameSource
    {
        public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset instant, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new FleetFrame(Guid.Empty, ++_sequence, instant,
                [new VehicleReport(7, _sequence, instant, -75.34, 41.09, 42, "swiftwater")]));
        }

        private long _sequence;
    }

    private sealed class ViewportSource : IFleetFrameSource, IFleetViewportControls
    {
        public List<ViewportExtent> Extents { get; } = [];

        public List<int?> Selections { get; } = [];

        public int LastReceivedVehicleCount => 1;

        public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset instant, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new FleetFrame(Guid.Empty, ++_sequence, instant,
                [new VehicleReport(7, _sequence, instant, -75.34, 41.09, 42, "swiftwater")]));
        }

        public Task SetViewportAsync(ViewportExtent extent, CancellationToken cancellationToken = default)
        {
            Extents.Add(extent);
            return Task.CompletedTask;
        }

        public Task SetSelectedVehicleAsync(int? vehicleId, CancellationToken cancellationToken = default)
        {
            Selections.Add(vehicleId);
            return Task.CompletedTask;
        }

        private long _sequence;
    }

    private sealed class ScenarioControlSource : IFleetFrameSource, IFleetScenarioControls
    {
        public bool AcceptsPreset { get; init; }

        public bool FailsReset { get; init; }

        public int ResetCount { get; private set; }

        public List<string> Presets { get; } = [];

        public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset instant,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new FleetFrame(Guid.Empty, ++_sequence, instant,
                [new VehicleReport(7, _sequence, instant, -75.34, 41.09, 42, "swiftwater")]));
        }

        public Task SetPresetAsync(string presetName, CancellationToken cancellationToken = default)
        {
            Presets.Add(presetName);
            return AcceptsPreset
                ? Task.CompletedTask
                : Task.FromException(new InvalidOperationException("The preset was refused."));
        }

        public Task PauseVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ResumeVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PauseFleetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResumeFleetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCount++;
            return FailsReset
                ? Task.FromException(new InvalidOperationException("The reset was refused."))
                : Task.CompletedTask;
        }

        public Task BreakConnectionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private long _sequence;
    }

    internal sealed class QuietMapView : MapView
    {
        public override Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }
}
