using FleetDashboard.Shared;
using FleetDashboard.Shared.Simulation;

namespace FleetDashboard.Client.Streaming;

/// <summary>Frames from the real <see cref="FleetSimulator"/> running inside the browser, for static
/// hosting where there is no server and no SignalR hub. It builds frames the way the server does, with
/// per-vehicle sequences, so <see cref="State.FleetDashboardState"/> sees the same data. It has no
/// background loop: each <see cref="GetFrameAsync"/> call runs whichever simulator and frame ticks have
/// come due on the supplied clock, which keeps it testable with a fake <see cref="TimeProvider"/>. The
/// scenario commands mirror <c>FleetHub</c>. Viewport filtering is server-side and keyed by connection,
/// so this source does not offer it, and there is no connection to report on.</summary>
public sealed class BrowserFleetFrameSource : IFleetFrameSource, IFleetScenarioControls
{
    public BrowserFleetFrameSource(TimeProvider clock)
    {
        _clock = clock;
        var store = new VehicleReportStore();
        _simulator = new FleetSimulator(clock, ScenarioSettings.Normal, store);
        _frameBuilder = new FleetFrameBuilder(store, clock);
    }

    public ValueTask<FleetFrame> GetFrameAsync(DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            // The server runs the reporting and frame clocks independently, so they are scheduled
            // independently here. An unset due time means "now", which also builds the first frame at
            // once, as the broadcaster does.
            if (observedAtUtc >= _nextReportAtUtc)
            {
                _simulator.Advance();
                _nextReportAtUtc = NextDue(_nextReportAtUtc, ReportInterval, observedAtUtc);
            }

            if (_currentFrame is null || observedAtUtc >= _nextFrameAtUtc)
            {
                _currentFrame = _frameBuilder.CreateFrame();
                _nextFrameAtUtc = NextDue(_nextFrameAtUtc, FrameInterval, observedAtUtc);
            }

            return ValueTask.FromResult(_currentFrame);
        }
    }

    public Task PauseVehicleAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        lock (_sync) _simulator.PauseVehicle(vehicleId);
        return Task.CompletedTask;
    }

    public Task ResumeVehicleAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        lock (_sync) _simulator.ResumeVehicle(vehicleId);
        return Task.CompletedTask;
    }

    public Task PauseFleetAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) _simulator.PauseFleet();
        return Task.CompletedTask;
    }

    public Task ResumeFleetAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync) _simulator.ResumeFleet();
        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            // Same order as FleetHub.Reset: the simulator first, so no frame pairs the new run ID with
            // the old run's high sequences. The new run's first frame is built here rather than on the
            // next tick, because the page rebuilds its state and reads a frame right after resetting.
            _simulator.Reset();
            _frameBuilder.StartNewRun();
            _currentFrame = _frameBuilder.CreateFrame();

            DateTimeOffset now = _clock.GetUtcNow();
            _nextReportAtUtc = now + ReportInterval;
            _nextFrameAtUtc = now + FrameInterval;
        }

        return Task.CompletedTask;
    }

    public Task SetPresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        // An unknown name throws ArgumentException, which the page reports as a command error. The
        // frame rate applies from the next scheduling decision, as the broadcaster re-reads it per tick.
        ScenarioSettings settings = ScenarioSettings.ByName(presetName);
        lock (_sync) _simulator.Configure(settings);
        return Task.CompletedTask;
    }

    /// <summary>Does nothing: there is no connection to break in the browser.</summary>
    public Task BreakConnectionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private TimeSpan ReportInterval => TimeSpan.FromSeconds(1d / _simulator.Settings.ReportsPerSecond);

    private TimeSpan FrameInterval => TimeSpan.FromSeconds(1d / _simulator.Settings.FramesPerSecond);

    /// <summary>Steps a due time forward by one interval, and skips ahead instead of replaying missed
    /// ticks when the caller is more than an interval late, as the server's timers do.</summary>
    private static DateTimeOffset NextDue(DateTimeOffset due, TimeSpan interval, DateTimeOffset now)
    {
        DateTimeOffset next = due + interval;
        return next <= now ? now + interval : next;
    }

    private readonly TimeProvider _clock;
    private readonly FleetSimulator _simulator;
    private readonly FleetFrameBuilder _frameBuilder;
    private readonly object _sync = new();
    private DateTimeOffset _nextReportAtUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextFrameAtUtc = DateTimeOffset.MinValue;
    private FleetFrame? _currentFrame;
}