namespace FleetDashboard.Shared;


/// <summary>
/// Pipeline modes selected by query string, with <c>ready</c> as the default.
/// The mode chooses the client render pipeline; it does not change the telemetry contract.
/// </summary>
public enum DemoMode
{
    Ready,

    /// <summary>The raw queue with no per-tick cap, so the map drains it one report at a time in arrival
    /// order. It defaults to the stress load and reaches the safety stop.</summary>
    Baseline,

    /// <summary>The raw queue with at most 250 records handed to the map per tick, which keeps it drained
    /// at the normal load.</summary>
    Pipeline,

    Latest,
    Batched,
    Scheduled,

    /// <summary>Implementation #5: the scheduled pipeline, but the server only sends what the
    /// map can see every frame and everything else at most once per second.</summary>
    Viewport,

    /// <summary>The scheduled pipeline again, named as the checkpoint for the stale and reconnect
    /// demonstrations. It changes nothing in the pipeline.</summary>
    Resilient
}


public static class DemoModeParser
{
    public static bool TryParse(string? value, out DemoMode mode)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "ready": mode = DemoMode.Ready; return true;
            case "pipeline": mode = DemoMode.Pipeline; return true;
            case "baseline": mode = DemoMode.Baseline; return true;
            case "latest": mode = DemoMode.Latest; return true;
            case "batched": mode = DemoMode.Batched; return true;
            case "scheduled": mode = DemoMode.Scheduled; return true;
            case "resilient": mode = DemoMode.Resilient; return true;
            case "viewport": mode = DemoMode.Viewport; return true;
            default: mode = DemoMode.Ready; return false;
        }
    }

    public static string Label(this DemoMode mode) => mode.ToString().ToLowerInvariant();


    /// <summary>Presenter-facing name; Label remains the URL and measurement token.</summary>
    public static string DisplayName(this DemoMode mode)
    {
        return mode switch
        {
            DemoMode.Ready => "Finished dashboard",
            DemoMode.Baseline => "Raw queue",
            DemoMode.Pipeline => "Raw queue with 250 record cap per tick",
            DemoMode.Latest => "Keep the latest per truck",
            DemoMode.Batched => "Batched ApplyEdits calls",
            DemoMode.Scheduled => "Batched ApplyEdits with 250 ms tick",
            DemoMode.Viewport => "Filter by view extent",
            DemoMode.Resilient => "Failure scenarios",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown demo mode.")
        };
    }


    /// <summary>What the presenter is watching in this mode.</summary>
    public static string WatchingLine(this DemoMode mode)
    {
        return mode switch
        {
            DemoMode.Ready => "Normal load: one report per truck, one ApplyEdits call every 250 ms. Same pipeline as scheduled and resilient.",
            DemoMode.Baseline => "Every report queued in arrival order and drawn one at a time, with no per-tick cap. Under stress it reaches the 20,000 safety stop.",
            DemoMode.Pipeline => "The same raw queue, but the map is handed at most 250 records a tick, which keeps it drained at the normal load.",
            DemoMode.Latest => "One waiting report per truck, latest wins. One ApplyEdits call per changed truck, about 59 a second.",
            DemoMode.Batched => "The same per-truck state, but one ApplyEdits call carries every changed truck: about 7 calls a second.",
            DemoMode.Scheduled => "The same batched ApplyEdits calls, on a 250 ms page tick instead of 100 ms: about 3.7 calls a second.",
            DemoMode.Viewport => "The server filters each frame by the map's view extent, plus the selected truck. The rest arrive once a second.",
            DemoMode.Resilient => "No pipeline change: this runs the same pipeline as ready and scheduled, with the failure controls at hand.",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown demo mode.")
        };
    }


    /// <summary>A short demonstration to run with this mode.</summary>
    public static string DemonstrationCue(this DemoMode mode)
    {
        return mode switch
        {
            DemoMode.Ready => "Open with no query string. Select Truck 007 and watch its marker step every 250 ms while Pending stays at 0.",
            DemoMode.Baseline => "Open ?mode=baseline, which applies the stress load. Watch Pending climb past 500 until the red safety stop banner appears.",
            DemoMode.Pipeline => "Open ?mode=pipeline at the normal load. Select Truck 007 and follow one report from Pending in the strip to its marker.",
            DemoMode.Latest => "Open ?mode=latest&preset=stress. Watch Pending hold at or under 500, with map calls a second near 59.",
            DemoMode.Batched => "Open ?mode=batched&preset=stress. Watch map calls a second drop from about 59 to about 7 with the same trucks moving.",
            DemoMode.Scheduled => "Open ?mode=scheduled&preset=stress. The tick lengthens from 100 ms to 250 ms, so watch map calls a second fall to about 3.7 with the motion unchanged.",
            DemoMode.Viewport => "Open ?mode=viewport&preset=stress, select Truck 007, then stop panning so the extent settles. Watch trucks per frame fall toward 191.",
            DemoMode.Resilient => "Open ?mode=resilient. Pause truck 007, and watch it go stale after 5 seconds, then break the connection and watch Reconnecting clear.",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown demo mode.")
        };
    }
}


/// <summary>
/// Validated workload settings. Rates are workload settings, not throughput promises. Each simulated
/// vehicle reports <see cref="ReportsPerSecond"/> times a second, and the server publishes
/// <see cref="FramesPerSecond"/> frames a second, each carrying the latest report from every vehicle.
/// The two clocks run independently, so records per second counts reports ingested by the server:
/// vehicles times report rate.
/// </summary>
public sealed record ScenarioSettings
{
    public const int MaxVehicleCount = 2000;
    public const int MaxReportsPerSecond = 20;
    public const int MaxFramesPerSecond = 20;

    public ScenarioSettings(string name, int vehicleCount, int reportsPerSecond, int framesPerSecond)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A scenario preset requires a name.", nameof(name));
        }

        if (vehicleCount is < 1 or > MaxVehicleCount)
        {
            throw new ArgumentOutOfRangeException(nameof(vehicleCount), vehicleCount,
                $"The roster holds 1 to {MaxVehicleCount} vehicles.");
        }

        if (reportsPerSecond is < 1 or > MaxReportsPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(reportsPerSecond), reportsPerSecond,
                $"Each simulated vehicle reports 1 to {MaxReportsPerSecond} times per second.");
        }

        if (framesPerSecond is < 1 or > MaxFramesPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond), framesPerSecond,
                $"The server publishes 1 to {MaxFramesPerSecond} frames per second.");
        }

        Name = name;
        VehicleCount = vehicleCount;
        ReportsPerSecond = reportsPerSecond;
        FramesPerSecond = framesPerSecond;
    }

    public string Name { get; }
    public int VehicleCount { get; }
    public int ReportsPerSecond { get; }
    public int FramesPerSecond { get; }
    public int RecordsPerSecond => VehicleCount * ReportsPerSecond;

    public static ScenarioSettings Normal { get; } = new("normal", 50, 2, 2);
    public static ScenarioSettings Stress { get; } = new("stress", 500, 10, 10);
    /// <summary>The E3 quiet-application control: the stress roster reporting once a second and
    /// published once a second, so a map drain still carries up to 500 dirty features while receive
    /// work drops tenfold.</summary>
    public static ScenarioSettings Quiet { get; } = new("quiet", 500, 1, 1);
    public static ScenarioSettings Ceiling { get; } = new("ceiling", 2000, 10, 10);

    /// <summary>Every named preset, lightest first, for the preview controls.</summary>
    public static IReadOnlyList<ScenarioSettings> All { get; } = [Normal, Quiet, Stress, Ceiling];

    public static ScenarioSettings ForMode(DemoMode mode) =>
        mode == DemoMode.Baseline ? Stress : Normal;

    public static ScenarioSettings ByName(string name) =>
        TryByName(name, out ScenarioSettings settings)
            ? settings
            : throw new ArgumentException(
                $"Unknown scenario preset '{name}'. Known presets: normal, stress, quiet, ceiling.", nameof(name));

    /// <summary>Non-throwing preset lookup, so query-string parsing can validate a preset name
    /// and fall back with a visible notice instead of faulting initialization.</summary>
    public static bool TryByName(string? name, out ScenarioSettings settings)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "normal": settings = Normal; return true;
            case "stress": settings = Stress; return true;
            case "quiet": settings = Quiet; return true;
            case "ceiling": settings = Ceiling; return true;
            default: settings = Normal; return false;
        }
    }
}
