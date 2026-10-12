using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server.Streaming;

/// <summary>Sends every client a snapshot of the store on the frame clock. The store already keeps
/// each vehicle's latest report, so nothing is queued: each tick builds the frame from what the store
/// holds at that moment and sends it. If a send outlasts the interval, the timer fires once as soon as
/// it finishes instead of replaying the missed ticks, so a slow send delays the next frame rather than
/// queuing a stale one. Connections that registered a viewport receive their filtered frame instead of
/// the full one.</summary>
public sealed class FleetBroadcaster : BackgroundService
{
    private readonly FleetFrameBuilder _frameBuilder;
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly ViewportSubscriptions _viewports;
    private ScenarioSettings _settings;

    public FleetBroadcaster(FleetFrameBuilder frameBuilder, ScenarioSettings settings,
        IHubContext<FleetHub> hubContext, ViewportSubscriptions viewports)
    {
        _frameBuilder = frameBuilder;
        _settings = settings;
        _hubContext = hubContext;
        _viewports = viewports;
    }

    /// <summary>Applies a preset's frame rate from the next tick on.</summary>
    public void Configure(ScenarioSettings settings)
    {
        Volatile.Write(ref _settings, settings);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FrameInterval());
        try
        {
            // Send a frame at once, so a client that connects or reconnects before the first tick gets a populated snapshot rather than the empty sequence-0 frame.
            await BroadcastAsync(_frameBuilder.CreateFrame(), stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await BroadcastAsync(_frameBuilder.CreateFrame(), stoppingToken);

                // Re-read the frame rate each tick so a preset change applies without a restart.
                TimeSpan interval = FrameInterval();
                if (timer.Period != interval) timer.Period = interval;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private TimeSpan FrameInterval() =>
        TimeSpan.FromSeconds(1d / Volatile.Read(ref _settings).FramesPerSecond);

    private async Task BroadcastAsync(FleetFrame frame, CancellationToken cancellationToken)
    {
        IReadOnlyList<KeyValuePair<string, ViewportFrameFilter>> viewports = _viewports.Snapshot();
        if (viewports.Count == 0)
        {
            await _hubContext.Clients.All.SendAsync(ClientMethod, frame, cancellationToken);
            return;
        }

        // Only the fan-out changes for viewport connections: the frame is complete, and every
        // other connection still gets the whole roster.
        string[] viewportIds = viewports.Select(viewport => viewport.Key).ToArray();
        List<Task> sends = [_hubContext.Clients.AllExcept(viewportIds).SendAsync(ClientMethod, frame, cancellationToken)];
        foreach ((string connectionId, ViewportFrameFilter filter) in viewports)
        {
            sends.Add(_hubContext.Clients.Client(connectionId)
                .SendAsync(ClientMethod, filter.Filter(frame), cancellationToken));
        }

        await Task.WhenAll(sends);
    }

    private const string ClientMethod = "ReceiveFleetFrame";
}
