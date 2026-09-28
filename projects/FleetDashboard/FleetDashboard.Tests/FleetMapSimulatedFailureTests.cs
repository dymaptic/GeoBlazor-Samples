using Bunit;
using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Core.Components.Views;
using FleetDashboard.Client.Pages;
using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


/// <summary>Drives the dashboard page rather than the map component, because the retry callback
/// re-renders <see cref="Home"/> and only that parent render pass exposes how the failure count
/// advances inside one cadence tick.</summary>
[TestClass]
public sealed class FleetMapSimulatedFailureTests
{
    [TestMethod]
    public async Task ASimulatedMapFailureCountsOncePerArrivingBatchNotOncePerRender()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        context.Services.AddSingleton(TimeProvider.System);
        var source = new AdvancingFrameSource();
        context.Services.AddSingleton<IFleetFrameSource>(source);
        context.ComponentFactories.Add<MapView>(() => new QuietMapView());
        var page = context.Render<Home>();

        page.FindAll("button").First(button => button.TextContent.Contains("Simulate map failure")).Click();

        // The source keeps serving the same frame, so several cadence ticks pass with nothing new
        // to apply and the retry callback re-renders the page each time.
        await Task.Delay(800);
        StringAssert.Contains(page.Markup, "attempt 1 of 3");
        Assert.IsFalse(page.Markup.Contains("Map updates paused"));
        StringAssert.Contains(page.Markup, "Failures: 1");

        // One arriving frame is one batch, so it is exactly one more attempt.
        source.AdvanceFrame();
        page.WaitForAssertion(() => StringAssert.Contains(page.Markup, "attempt 2 of 3"));
        StringAssert.Contains(page.Markup, "Failures: 2");

        // The third batch reaches the bound and pauses map updates, as the bound promises.
        source.AdvanceFrame();
        page.WaitForAssertion(() => StringAssert.Contains(page.Markup, "Map updates paused"));
        StringAssert.Contains(page.Markup, "Failures: 3");
    }

    private sealed class QuietMapView : MapView
    {
        public override Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    /// <summary>A connected hub that accepts scenario commands and only publishes a new frame when
    /// the test asks for one, so batch arrival is the only thing that can advance the map.</summary>
    private sealed class AdvancingFrameSource : IFleetFrameSource, IFleetScenarioControls
    {
        public void AdvanceFrame() => _frameSequence++;

        public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset instant,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new FleetFrame(Guid.Empty, _frameSequence, instant,
                [new VehicleReport(7, _frameSequence, instant, -75.34, 41.09, 42, "swiftwater")]));
        }

        public Task SetPresetAsync(string presetName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PauseVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ResumeVehicleAsync(int vehicleId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PauseFleetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResumeFleetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task BreakConnectionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private long _frameSequence;
    }
}
