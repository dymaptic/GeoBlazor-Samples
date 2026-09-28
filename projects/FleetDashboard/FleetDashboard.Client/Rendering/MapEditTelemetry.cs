namespace FleetDashboard.Client.Rendering;


/// <summary>How the map adapter submits pending vehicle changes.</summary>
public enum MapEditGranularity
{
    /// <summary>One awaited collection edit per drained batch.</summary>
    Collection,

    /// <summary>One awaited edit per vehicle, submitted serially.</summary>
    Individual
}


/// <summary>Observation of one successful map edit call.</summary>
public sealed record MapEditTelemetry(int FeatureCount, double ApplyMilliseconds);
