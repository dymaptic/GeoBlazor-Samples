namespace FleetDashboard.Shared;


/// <summary>
/// Pipeline modes selected by query string, with <c>ready</c> as the default.
/// The mode chooses the client render pipeline; it does not change the telemetry contract.
/// </summary>
public enum DemoMode
{
    Ready,
    Pipeline,
    Baseline,
    Latest,
    Batched,
    Scheduled,
    Resilient,

    /// <summary>Implementation #5: the scheduled pipeline, but the server only sends what the
    /// map can see every frame and everything else at most once per second.</summary>
    Viewport
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
            DemoMode.Pipeline => "Trace the data path",
            DemoMode.Baseline => "Keep every report",
            DemoMode.Latest => "Keep the newest per truck",
            DemoMode.Batched => "One call, all trucks",
            DemoMode.Scheduled => "Read every 250 ms",
            DemoMode.Viewport => "Send only what is in view",
            DemoMode.Resilient => "Failure scenarios",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown demo mode.")
        };
    }


    /// <summary>What the presenter is watching in this mode.</summary>
    public static string WatchingLine(this DemoMode mode)
    {
        return mode switch
        {
            DemoMode.Ready => "Normal load: one position per truck, one collection map edit every 250 ms.",
            DemoMode.Pipeline => "Every report queued in arrival order, then up to 250 edits per 100 ms tick, one per truck.",
            DemoMode.Baseline => "Every report drawn one at a time. Under stress the queue grows to the 20,000 safety stop.",
            DemoMode.Latest => "One waiting report per truck, newest wins. One edit per changed truck, about 59 a second.",
            DemoMode.Batched => "Newest per truck, but every change rides in one map call per tick, about 7 calls a second.",
            DemoMode.Scheduled => "The 250 ms design under stress: one map call per tick, about 3.7 calls a second.",
            DemoMode.Viewport => "Trucks inside the map view arrive each frame; the rest arrive at most once a second.",
            DemoMode.Resilient => "Silent trucks go stale after 5 seconds, and a broken connection can be reconnected.",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown demo mode.")
        };
    }


    /// <summary>A short demonstration to run with this mode.</summary>
    public static string DemonstrationCue(this DemoMode mode)
    {
        return mode switch
        {
            DemoMode.Ready => "Open with no query string. Select Truck 007 and watch its marker step every 250 ms while Pending stays at 0.",
            DemoMode.Pipeline => "Open ?mode=pipeline, then select Truck 007 and follow one report from Pending in the strip to its marker on the map.",
            DemoMode.Baseline => "Open ?mode=baseline for stress load. Watch Pending climb past 500 until the red safety stop banner appears.",
            DemoMode.Latest => "Open ?mode=latest&preset=stress. Watch Pending hold at or under 500, with map calls a second near 59.",
            DemoMode.Batched => "Open ?mode=batched&preset=stress. Watch map calls a second drop from about 59 to about 7 with the same trucks moving.",
            DemoMode.Scheduled => "Open ?mode=scheduled&preset=stress. Watch map calls a second fall to about 3.7 with the truck motion unchanged.",
            DemoMode.Viewport => "Open ?mode=viewport&preset=stress, select Truck 007, then stop panning. Watch trucks per frame fall from 500 toward 191.",
            DemoMode.Resilient => "Open ?mode=resilient. Pause truck 007: stale after 5 seconds. Break the connection and watch Reconnecting clear.",
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
