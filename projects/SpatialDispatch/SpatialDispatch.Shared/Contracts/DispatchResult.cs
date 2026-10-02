namespace SpatialDispatch.Shared.Contracts;

/// <summary>
///     The answer to the dispatcher's question: which territory covers this job, and which available
///     technician assigned there is closest.
/// </summary>
/// <remarks>
///     <see cref="Candidates" /> is not needed to make the assignment. It exists because the presentation
///     has to show availability and territory eliminating plausible alternatives; without it the dashboard
///     would only assert a winner.
/// </remarks>
public sealed record DispatchResult(
    int JobId,
    Territory ContainingTerritory,
    TechnicianCandidate RecommendedTechnician,
    double StraightLineDistanceMeters,
    IReadOnlyList<TechnicianCandidate> Candidates);
