using FleetDashboard.Client.Rendering;
using FleetDashboard.Shared;


namespace FleetDashboard.Client.Streaming;


/// <summary>Mode-specific pipeline decisions shared by the dashboard page and map adapter.</summary>
public static class DemoModes
{
    /// <summary>Capture cadence for the map pipeline: latest/batched and the raw-queue pipeline
    /// drain every 100 ms; scheduled and the finished dashboard run at 250 ms.</summary>
    public static TimeSpan MapCadence(this DemoMode mode) => mode switch
    {
        DemoMode.Latest or DemoMode.Batched or DemoMode.Pipeline or DemoMode.Baseline
            => TimeSpan.FromMilliseconds(100),
        _ => TimeSpan.FromMilliseconds(250)
    };

    /// <summary>Pipeline and baseline keep every observation in a raw queue instead of the
    /// bounded latest-per-vehicle state.</summary>
    public static bool UsesRawQueue(this DemoMode mode) =>
        mode is DemoMode.Pipeline or DemoMode.Baseline;

    public static MapEditGranularity EditGranularity(this DemoMode mode) => mode switch
    {
        DemoMode.Pipeline or DemoMode.Baseline or DemoMode.Latest
            => MapEditGranularity.Individual,
        _ => MapEditGranularity.Collection
    };

    public static ScenarioSettings Preset(this DemoMode mode) => ScenarioSettings.ForMode(mode);
}
