using System.Threading.Channels;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server.Streaming;

/// <summary>Publishes a frame to every client on its own clock, independent of how often the
/// vehicles report. A frame carries whatever the store holds at that moment.</summary>
public sealed class FleetBroadcaster : BackgroundService
{
    private readonly FleetFrameBuilder _frameBuilder;
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly Channel<FleetFrame> _frames;
    private ScenarioSettings _settings;
    private long _droppedFrames;

    public FleetBroadcaster(FleetFrameBuilder frameBuilder, ScenarioSettings settings,
        IHubContext<FleetHub> hubContext)
    {
        _frameBuilder = frameBuilder;
        _settings = settings;
        _hubContext = hubContext;
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
            await _hubContext.Clients.All.SendAsync("ReceiveFleetFrame", frame, cancellationToken);
        }
    }
}
