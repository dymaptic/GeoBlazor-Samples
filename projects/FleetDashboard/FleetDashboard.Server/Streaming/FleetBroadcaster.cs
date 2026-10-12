using System.Threading.Channels;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server.Streaming;

/// <summary>Publishes a frame to every client on its own clock, independent of how often the
/// vehicles report. A frame carries whatever the store holds at that moment. Connections that
/// registered a viewport receive their filtered frame instead of the full one.</summary>
public sealed class FleetBroadcaster : BackgroundService
{
    private readonly FleetFrameBuilder _frameBuilder;
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly ViewportSubscriptions _viewports;
    private readonly Channel<FleetFrame> _frames;
    private ScenarioSettings _settings;
    private long _droppedFrames;

    public FleetBroadcaster(FleetFrameBuilder frameBuilder, ScenarioSettings settings,
        IHubContext<FleetHub> hubContext, ViewportSubscriptions viewports)
    {
        _frameBuilder = frameBuilder;
        _settings = settings;
        _hubContext = hubContext;
        _viewports = viewports;
        _frames = Channel.CreateBounded<FleetFrame>(new BoundedChannelOptions(2)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        }, _ => Interlocked.Increment(ref _droppedFrames));
    }

    public long DroppedFrames => Interlocked.Read(ref _droppedFrames);

    /// <summary>Applies a preset's frame rate from the next tick on.</summary>
    public void Configure(ScenarioSettings settings)
    {
        Volatile.Write(ref _settings, settings);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        Task producer = ProduceAsync(cancellation.Token);
        Task consumer = BroadcastAsync(cancellation.Token);

        await Task.WhenAny(producer, consumer);
        cancellation.Cancel();

        try
        {
            await Task.WhenAll(producer, consumer);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProduceAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _frames.Writer.TryWrite(_frameBuilder.CreateFrame());

                // Re-read the frame rate each tick so a preset change applies without a restart.
                await Task.Delay(TimeSpan.FromSeconds(1d / Volatile.Read(ref _settings).FramesPerSecond),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _frames.Writer.TryComplete();
        }
    }

    private async Task BroadcastAsync(CancellationToken cancellationToken)
    {
        await foreach (FleetFrame frame in _frames.Reader.ReadAllAsync(cancellationToken))
        {
            IReadOnlyList<KeyValuePair<string, ViewportFrameFilter>> viewports = _viewports.Snapshot();
            if (viewports.Count == 0)
            {
                await _hubContext.Clients.All.SendAsync(ClientMethod, frame, cancellationToken);
                continue;
            }

            // Only the fan-out changes for viewport connections: the upstream channel still holds
            // complete frames, and every other connection still gets the whole roster.
            string[] viewportIds = viewports.Select(viewport => viewport.Key).ToArray();
            List<Task> sends = [_hubContext.Clients.AllExcept(viewportIds).SendAsync(ClientMethod, frame, cancellationToken)];
            foreach ((string connectionId, ViewportFrameFilter filter) in viewports)
            {
                sends.Add(_hubContext.Clients.Client(connectionId)
                    .SendAsync(ClientMethod, filter.Filter(frame), cancellationToken));
            }

            await Task.WhenAll(sends);
        }
    }

    private const string ClientMethod = "ReceiveFleetFrame";
}
