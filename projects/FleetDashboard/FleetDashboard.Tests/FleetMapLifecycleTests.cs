using Bunit;
using dymaptic.GeoBlazor.Core;
using dymaptic.GeoBlazor.Core.Components.Geometries;
using dymaptic.GeoBlazor.Core.Components.Views;
using FleetDashboard.Client.Components;
using FleetDashboard.Client.Rendering;
using FleetDashboard.Client.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetMapLifecycleTests
{
    [TestMethod]
    public async Task TelemetryDoesNotResupplyTheInitialMapSource()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<FleetMap>();
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));
        int initialRenderCount = mapView.ParameterSets;

        map.Render(parameters => parameters.Add(p => p.Vehicles, new VehicleListItem[]
        {
            new(7, "swiftwater", "Swiftwater loop", -75.34, 41.09, 42,
                DateTimeOffset.UtcNow, VehicleStatus.Stale, 6)
        }));

        Assert.AreEqual(initialRenderCount, mapView.ParameterSets);
    }

    [TestMethod]
    public async Task MapReadinessWaitsForTheFirstRenderedView()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        bool initialized = false;
        var map = context.Render<FleetMap>(p => p.Add(m => m.Initialized, () => initialized = true));

        await map.InvokeAsync(() => mapView.OnViewInitialized.InvokeAsync());
        Assert.IsFalse(initialized);

        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));
        Assert.IsTrue(initialized);
    }

    [TestMethod]
    public async Task RepeatedRenderedEventsDoNotOverlapRouteInitialization()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<HoldingFleetMap>();

        Task first = map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));
        Task second = map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));
        int starts = map.Instance.RouteInitializations;
        map.Instance.ReleaseRoutes.SetResult();
        await Task.WhenAll(first, second);

        Assert.AreEqual(1, starts);
    }

    [TestMethod]
    public async Task DeltaUpdatesDoNotTreatTheCurrentVehicleViewAsTheMapRoster()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        VehicleListItem first = Vehicle(1);
        VehicleListItem second = Vehicle(2);
        var map = context.Render<TrackingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { first, second }));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        VehicleListItem changedFirst = first with { SpeedKph = 54 };
        map.Render(parameters => parameters
            .Add(component => component.Vehicles, new[] { changedFirst, second })
            .Add(component => component.UpdatedVehicles, new[] { changedFirst })
            .Add(component => component.RemovedVehicleIds, new[] { 2 })
            .Add(component => component.UpdateBatchVersion, 1L));

        map.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new[] { 1 }, map.Instance.UpdatedIds.ToArray());
            CollectionAssert.AreEqual(new[] { 2 }, map.Instance.RemovedIds.ToArray());
        });
    }

    [TestMethod]
    public async Task CollectionGranularitySubmitsOneEditPerDrainedBatch()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<TrackingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1), Vehicle(2), Vehicle(3) })
            .Add(component => component.EditGranularity, MapEditGranularity.Collection));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles,
                new[] { Vehicle(1), Vehicle(2), Vehicle(3) })
            .Add(component => component.UpdateBatchVersion, 1L));

        map.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { 3 }, map.Instance.BatchSizes.ToArray()));
    }

    [TestMethod]
    public async Task IndividualGranularitySubmitsOneEditPerVehicle()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<TrackingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1), Vehicle(2), Vehicle(3) })
            .Add(component => component.EditGranularity, MapEditGranularity.Individual));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles,
                new[] { Vehicle(1), Vehicle(2), Vehicle(3) })
            .Add(component => component.UpdateBatchVersion, 1L));

        map.WaitForAssertion(() =>
            CollectionAssert.AreEqual(new[] { 1, 1, 1 }, map.Instance.BatchSizes.ToArray()));
    }

    [TestMethod]
    public async Task TheDrainCapLimitsEditCallsPerPassAndTheRemainderDrainsOnTheNextBatch()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<TrackingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles,
                new[] { Vehicle(1), Vehicle(2), Vehicle(3), Vehicle(4), Vehicle(5) })
            .Add(component => component.EditGranularity, MapEditGranularity.Individual)
            .Add(component => component.MaxEditsPerDrain, 2));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles,
                new[] { Vehicle(1), Vehicle(2), Vehicle(3), Vehicle(4), Vehicle(5) })
            .Add(component => component.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() =>
            CollectionAssert.AreEqual(new[] { 1, 1 }, map.Instance.BatchSizes.ToArray()));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles,
                new[] { Vehicle(3), Vehicle(4), Vehicle(5) })
            .Add(component => component.UpdateBatchVersion, 2L));
        map.WaitForAssertion(() =>
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, map.Instance.BatchSizes.ToArray()));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(5) })
            .Add(component => component.UpdateBatchVersion, 3L));
        map.WaitForAssertion(() =>
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1 }, map.Instance.BatchSizes.ToArray()));
    }

    [TestMethod]
    public async Task PartialEditFailuresRetryToTheLimitThenPause()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        int failed = 0;
        string? message = null;
        List<int> retryAttempts = [];
        var map = context.Render<FailingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1) })
            .Add(component => component.EditFailed, (string text) => { failed++; message = text; })
            .Add(component => component.EditRetrying, (int attempt) => retryAttempts.Add(attempt)));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(1) })
            .Add(component => component.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.Attempts));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(1) })
            .Add(component => component.UpdateBatchVersion, 2L));
        map.WaitForAssertion(() => Assert.AreEqual(2, map.Instance.Attempts));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(1) })
            .Add(component => component.UpdateBatchVersion, 3L));
        map.WaitForAssertion(() => Assert.AreEqual(3, map.Instance.Attempts));

        Assert.AreEqual(1, failed);
        StringAssert.Contains(message!, "boom");
        CollectionAssert.AreEqual(new[] { 1, 2 }, retryAttempts.ToArray());
    }

    [TestMethod]
    public async Task AFailedBatchReconcilesPendingVehiclesWithTheirNewestState()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        int failed = 0;
        var map = context.Render<FlakyFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1) })
            .Add(component => component.EditFailed, (string _) => failed++));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(1) with { SpeedKph = 54 } })
            .Add(component => component.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.Attempts));

        VehicleListItem newest = Vehicle(1) with { SpeedKph = 88 };
        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { newest })
            .Add(component => component.UpdateBatchVersion, 2L));
        map.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, map.Instance.Attempts);
            Assert.AreEqual(88, map.Instance.LastApplied.Single().SpeedKph);
        });

        Assert.AreEqual(0, failed);
    }

    [TestMethod]
    public async Task DisposingDuringAnInFlightEditCompletesWithoutThrowing()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<HoldingEditFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1) }));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(1) })
            .Add(component => component.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() => Assert.IsTrue(map.Instance.EditStarted.Task.IsCompleted));

        await map.Instance.DisposeAsync();
        map.Instance.ReleaseEdit.TrySetResult();
    }

    [TestMethod]
    public async Task SelectingAVehicleZoomsOnceAndTelemetryDoesNotRecenter()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<ZoomTrackingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1), Vehicle(2) }));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters.Add(component => component.SelectedVehicleId, 1));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.ZoomedExtents.Count));

        map.Render(parameters => parameters
            .Add(component => component.Vehicles, new[] { Vehicle(1) with { SpeedKph = 88 }, Vehicle(2) })
            .Add(component => component.UpdatedVehicles, new[] { Vehicle(1) with { SpeedKph = 88 } })
            .Add(component => component.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, map.Instance.ZoomedExtents.Count);
            CollectionAssert.AreEqual(new[] { 1 }, map.Instance.AppliedIds.ToArray());
        });

        map.Render(parameters => parameters.Add(component => component.SelectedVehicleId, 2));
        map.WaitForAssertion(() => Assert.AreEqual(2, map.Instance.ZoomedExtents.Count));
    }

    [TestMethod]
    public async Task ZoomExtentCoversTheSelectedVehicle()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        VehicleListItem selected = Vehicle(7);
        var map = context.Render<ZoomTrackingFleetMap>(parameters => parameters
            .Add(component => component.Vehicles, new[] { selected }));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters.Add(component => component.SelectedVehicleId, 7));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.ZoomedExtents.Count));

        Extent extent = map.Instance.ZoomedExtents.Single();
        Assert.IsTrue(extent.Xmin < selected.Longitude && extent.Xmax > selected.Longitude);
        Assert.IsTrue(extent.Ymin < selected.Latitude && extent.Ymax > selected.Latitude);
        Assert.AreEqual(4326, extent.SpatialReference?.Wkid);
    }

    [TestMethod]
    public async Task OrderedGranularityAppliesEveryReportInArrivalOrder()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        List<int> counts = [];
        var map = context.Render<OrderTrackingFleetMap>(parameters => parameters
            .Add(c => c.Vehicles, new[] { Vehicle(7) })
            .Add(c => c.EditGranularity, MapEditGranularity.Ordered)
            .Add(c => c.PendingReportsChanged, (Action<int>)counts.Add));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(c => c.UpdatedVehicles, new[] { Vehicle(7) with { SpeedKph = 10 },
                Vehicle(7) with { SpeedKph = 20 }, Vehicle(7) with { SpeedKph = 30 } })
            .Add(c => c.UpdateBatchVersion, 1L));

        map.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new double[] { 10, 20, 30 }, map.Instance.AppliedSpeeds.ToArray());
            Assert.AreEqual(0, counts[^1]);
        });
    }

    [TestMethod]
    public async Task OrderedGranularityRetriesAFailedReportBeforeLaterOnes()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        var map = context.Render<FlakyOrderedFleetMap>(parameters => parameters
            .Add(c => c.Vehicles, new[] { Vehicle(7) })
            .Add(c => c.EditGranularity, MapEditGranularity.Ordered));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(c => c.UpdatedVehicles, new[] { Vehicle(7) with { SpeedKph = 10 },
                Vehicle(7) with { SpeedKph = 20 } })
            .Add(c => c.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.Attempts));

        // An empty batch starts the next drain, which must retry the failed report first.
        map.Render(parameters => parameters
            .Add(c => c.UpdatedVehicles, Array.Empty<VehicleListItem>())
            .Add(c => c.UpdateBatchVersion, 2L));

        map.WaitForAssertion(() =>
            CollectionAssert.AreEqual(new double[] { 10, 20 }, map.Instance.AppliedSpeeds.ToArray()));
    }

    [TestMethod]
    public async Task DiscardVersionClearsTheOrderedQueue()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        List<int> counts = [];
        var map = context.Render<BlockingOrderedFleetMap>(parameters => parameters
            .Add(c => c.Vehicles, new[] { Vehicle(7) })
            .Add(c => c.EditGranularity, MapEditGranularity.Ordered)
            .Add(c => c.PendingReportsChanged, (Action<int>)counts.Add));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(c => c.UpdatedVehicles, new[] { Vehicle(7) with { SpeedKph = 10 },
                Vehicle(7) with { SpeedKph = 20 }, Vehicle(7) with { SpeedKph = 30 } })
            .Add(c => c.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.Started));

        // Discard while the first edit is in flight, then let that edit finish.
        map.Render(parameters => parameters.Add(c => c.DiscardVersion, 1));
        await map.InvokeAsync(() => map.Instance.Gate.SetResult());

        map.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, counts[^1]);
            CollectionAssert.AreEqual(new double[] { 10 }, map.Instance.AppliedSpeeds.ToArray());
        });
    }

    [TestMethod]
    public async Task ADiscardNeverDropsANewerOrderedReportQueuedDuringTheInFlightEdit()
    {
        await using var context = new BunitContext();
        IConfiguration config = new ConfigurationBuilder().Build();
        context.Services.AddSingleton(config);
        context.Services.AddGeoBlazor(config);
        var mapView = new RecordingMapView();
        context.ComponentFactories.Add<MapView>(() => mapView);
        List<int> counts = [];
        int failures = 0;
        var map = context.Render<BlockingOrderedFleetMap>(parameters => parameters
            .Add(c => c.Vehicles, new[] { Vehicle(7) })
            .Add(c => c.EditGranularity, MapEditGranularity.Ordered)
            .Add(c => c.PendingReportsChanged, (Action<int>)counts.Add)
            .Add(c => c.EditRetrying, (int _) => failures++)
            .Add(c => c.EditFailed, (string _) => failures++));
        await map.InvokeAsync(() => mapView.OnViewRendered.InvokeAsync(mapView.Id));

        map.Render(parameters => parameters
            .Add(c => c.UpdatedVehicles, new[] { Vehicle(7) with { SpeedKph = 10 },
                Vehicle(7) with { SpeedKph = 20 }, Vehicle(7) with { SpeedKph = 30 } })
            .Add(c => c.UpdateBatchVersion, 1L));
        map.WaitForAssertion(() => Assert.AreEqual(1, map.Instance.Started));

        // Discard, then queue a newer report while the first edit is still in flight.
        map.Render(parameters => parameters.Add(c => c.DiscardVersion, 1));
        map.Render(parameters => parameters
            .Add(c => c.UpdatedVehicles, new[] { Vehicle(7) with { SpeedKph = 40 } })
            .Add(c => c.UpdateBatchVersion, 2L));
        await map.InvokeAsync(() => map.Instance.Gate.SetResult());

        map.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(new double[] { 10, 40 }, map.Instance.AppliedSpeeds.ToArray());
            Assert.AreEqual(0, counts[^1]);
        });
        Assert.AreEqual(0, failures);
    }

    private static VehicleListItem Vehicle(int vehicleId) => new(vehicleId, "swiftwater", "Swiftwater loop",
        -75.34, 41.09, 42, DateTimeOffset.UtcNow, VehicleStatus.Moving, 0);

    private sealed class OrderTrackingFleetMap : FleetMap
    {
        public List<double> AppliedSpeeds { get; } = [];

        protected override Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            AppliedSpeeds.AddRange(updatedVehicles.Select(v => v.SpeedKph));
            return Task.CompletedTask;
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class FlakyOrderedFleetMap : FleetMap
    {
        public int Attempts { get; private set; }
        public List<double> AppliedSpeeds { get; } = [];

        protected override Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            Attempts++;
            if (Attempts == 1) throw new InvalidOperationException("The first edit fails.");
            AppliedSpeeds.AddRange(updatedVehicles.Select(v => v.SpeedKph));
            return Task.CompletedTask;
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class BlockingOrderedFleetMap : FleetMap
    {
        public TaskCompletionSource Gate { get; } = new();
        public int Started { get; private set; }
        public List<double> AppliedSpeeds { get; } = [];

        protected override async Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            Started++;
            await Gate.Task;
            AppliedSpeeds.AddRange(updatedVehicles.Select(v => v.SpeedKph));
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class HoldingFleetMap : FleetMap
    {
        public TaskCompletionSource ReleaseRoutes { get; } = new();
        public int RouteInitializations { get; private set; }

        protected override Task AddRoutes()
        {
            RouteInitializations++;
            return ReleaseRoutes.Task;
        }
    }

    private sealed class TrackingFleetMap : FleetMap
    {
        public IReadOnlyList<int> UpdatedIds { get; private set; } = [];
        public IReadOnlyList<int> RemovedIds { get; private set; } = [];
        public IReadOnlyList<int> BatchSizes { get; private set; } = [];

        protected override Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            UpdatedIds = updatedVehicles.Select(vehicle => vehicle.VehicleId).ToArray();
            RemovedIds = removedVehicleIds.ToArray();
            BatchSizes = [..BatchSizes, updatedVehicles.Count + removedVehicleIds.Count];
            return Task.CompletedTask;
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class FailingFleetMap : FleetMap
    {
        public int Attempts { get; private set; }

        protected override Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            Attempts++;
            throw new InvalidOperationException("boom");
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class FlakyFleetMap : FleetMap
    {
        public int Attempts { get; private set; }
        public IReadOnlyList<VehicleListItem> LastApplied { get; private set; } = [];

        protected override Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            Attempts++;
            if (Attempts == 1) throw new InvalidOperationException("transient");
            LastApplied = updatedVehicles;
            return Task.CompletedTask;
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class HoldingEditFleetMap : FleetMap
    {
        public TaskCompletionSource EditStarted { get; } = new();
        public TaskCompletionSource ReleaseEdit { get; } = new();

        protected override async Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            EditStarted.TrySetResult();
            await ReleaseEdit.Task;
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class ZoomTrackingFleetMap : FleetMap
    {
        public List<Extent> ZoomedExtents { get; } = [];
        public List<int> AppliedIds { get; private set; } = [];

        protected override Task GoToExtent(Extent extent)
        {
            ZoomedExtents.Add(extent);
            return Task.CompletedTask;
        }

        protected override Task ApplyVehicleDelta(IReadOnlyList<VehicleListItem> updatedVehicles,
            IReadOnlyList<int> removedVehicleIds)
        {
            AppliedIds = updatedVehicles.Select(vehicle => vehicle.VehicleId).ToList();
            return Task.CompletedTask;
        }

        protected override Task AddRoutes() => Task.CompletedTask;

        protected override bool HasVehicleLayer => true;
    }

    private sealed class RecordingMapView : MapView
    {
        public int ParameterSets { get; private set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            ParameterSets++;
            return Task.CompletedTask;
        }
    }
}
