using FleetDashboard.Server;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;


namespace FleetDashboard.Tests;


/// <summary>A hub context that records every frame sent to clients in order and can optionally
/// hold the first send, so server tests can run the broadcaster without a SignalR host.</summary>
internal sealed class RecordingHubContext : IHubContext<FleetHub>
{
    private readonly RecordingClientProxy _proxy = new();

    public Task<FleetFrame> NextFrame => _proxy.NextFrame.Task;

    public string? MethodName => _proxy.MethodName;

    /// <summary>Frames in the order they were sent.</summary>
    public IReadOnlyList<FleetFrame> SentFrames => _proxy.SentFrames;

    /// <summary>When set, the first send waits for this before completing, to stand in for a slow client.</summary>
    public TaskCompletionSource? HoldFirstSend
    {
        get => _proxy.HoldFirstSend;
        set => _proxy.HoldFirstSend = value;
    }

    public IHubClients Clients => new RecordingHubClients(_proxy);

    public IGroupManager Groups => new RecordingGroupManager();

    private sealed class RecordingHubClients(IClientProxy proxy) : IHubClients
    {
        public IClientProxy All => proxy;

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;

        public IClientProxy Client(string connectionId) => proxy;

        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;

        public IClientProxy Group(string groupName) => proxy;

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => proxy;

        public IClientProxy User(string userId) => proxy;

        public IClientProxy Users(IReadOnlyList<string> userIds) => proxy;
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        private readonly object _gate = new();
        private readonly List<FleetFrame> _frames = [];

        public TaskCompletionSource<FleetFrame> NextFrame { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string? MethodName { get; private set; }

        public TaskCompletionSource? HoldFirstSend { get; set; }

        public IReadOnlyList<FleetFrame> SentFrames
        {
            get { lock (_gate) return _frames.ToArray(); }
        }

        public async Task SendCoreAsync(string methodName, object?[] args,
            CancellationToken cancellationToken = default)
        {
            MethodName = methodName;
            if (args.Length != 1 || args[0] is not FleetFrame frame) return;

            int sent;
            lock (_gate)
            {
                _frames.Add(frame);
                sent = _frames.Count;
            }

            NextFrame.TrySetResult(frame);
            if (sent == 1 && HoldFirstSend is { } hold) await hold.Task.WaitAsync(cancellationToken);
        }
    }
}
