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
            default: mode = DemoMode.Ready; return false;
        }
    }

    public static string Label(this DemoMode mode) => mode.ToString().ToLowerInvariant();
}


/// <summary>
/// Validated simulator workload settings. Rates are workload settings, not throughput promises;
/// every frame carries the complete roster, so records per second is vehicles times frame rate.
/// </summary>
public sealed record ScenarioSettings
{
    public const int MaxVehicleCount = 2000;
    public const int MaxFramesPerSecond = 20;

    public ScenarioSettings(string name, int vehicleCount, int framesPerSecond)
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

        if (framesPerSecond is < 1 or > MaxFramesPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond), framesPerSecond,
                $"The simulator publishes 1 to {MaxFramesPerSecond} frames per second.");
        }

        Name = name;
        VehicleCount = vehicleCount;
        FramesPerSecond = framesPerSecond;
    }

    public string Name { get; }
    public int VehicleCount { get; }
    public int FramesPerSecond { get; }
    public int RecordsPerSecond => VehicleCount * FramesPerSecond;

    public static ScenarioSettings Normal { get; } = new("normal", 50, 2);
    public static ScenarioSettings Stress { get; } = new("stress", 500, 10);
    /// <summary>The E3 quiet-application control: the stress roster at one frame per second, so a
    /// map drain still carries up to 500 dirty features while receive work drops tenfold.</summary>
    public static ScenarioSettings Quiet { get; } = new("quiet", 500, 1);
    public static ScenarioSettings Ceiling { get; } = new("ceiling", 2000, 10);

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
