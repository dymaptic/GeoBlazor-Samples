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
