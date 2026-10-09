using System.Threading.Channels;
using FleetDashboard.Server.Simulation;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace FleetDashboard.Server.Streaming;

public sealed class FleetBroadcaster : BackgroundService
{
    private readonly FleetSimulator _simulator;
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly ViewportSubscriptions _viewports;
    private readonly Channel<FleetFrame> _frames;
    private long _droppedFrames;

    public FleetBroadcaster(FleetSimulator simulator, IHubContext<FleetHub> hubContext,
        ViewportSubscriptions viewports)
    {
        _simulator = simulator;
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
                _frames.Writer.TryWrite(_simulator.Advance());

                // Re-read the frame rate each tick so a preset change applies without a restart.
                await Task.Delay(TimeSpan.FromSeconds(1d / _simulator.Settings.FramesPerSecond),
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
