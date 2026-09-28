using FleetDashboard.Server;
using FleetDashboard.Server.Simulation;
using FleetDashboard.Server.Streaming;
using FleetDashboard.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetBroadcasterTests
{
    [TestMethod]
    public async Task BroadcastsSimulatorFramesOnTheClientMethodContract()
    {
        var simulator = new FleetSimulator(TimeProvider.System, ScenarioSettings.Normal);
        var hubContext = new RecordingHubContext();
        var broadcaster = new FleetBroadcaster(simulator, hubContext);

        await broadcaster.StartAsync(CancellationToken.None);
        FleetFrame frame = await hubContext.NextFrame.WaitAsync(TimeSpan.FromSeconds(15));
        await broadcaster.StopAsync(CancellationToken.None);

        Assert.AreEqual("ReceiveFleetFrame", hubContext.MethodName);
        Assert.AreEqual(ScenarioSettings.Normal.VehicleCount, frame.Vehicles.Count);
    }

    private sealed class RecordingHubContext : IHubContext<FleetHub>
    {
        private readonly RecordingClientProxy _proxy = new();

        public Task<FleetFrame> NextFrame => _proxy.NextFrame.Task;

        public string? MethodName => _proxy.MethodName;

        public IHubClients Clients => new RecordingHubClients(_proxy);

        public IGroupManager Groups => new RecordingGroupManager();
    }

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
        public TaskCompletionSource<FleetFrame> NextFrame { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string? MethodName { get; private set; }

        public Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            MethodName = methodName;
            if (args.Length == 1 && args[0] is FleetFrame frame) NextFrame.TrySetResult(frame);
            return Task.CompletedTask;
        }
    }
}
