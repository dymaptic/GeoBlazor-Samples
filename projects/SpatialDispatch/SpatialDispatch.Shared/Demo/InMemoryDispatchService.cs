using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Shared.Demo;

/// <summary>
///     Answers the dispatch question from the demo contract records, with no database.
/// </summary>
/// <remarks>
///     The filtering and ranking here are real: availability and territory assignment eliminate candidates
///     before distance orders whoever remains. Only the containment lookup and the distances are canned, and
///     Task 3 replaces both with translated SQL Server spatial queries.
/// </remarks>
public sealed class InMemoryDispatchService : IDispatchService
{
    public Task<IReadOnlyList<Job>> GetOpenJobsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(DemoContract.Jobs);

    public Task<DispatchResult> GetDispatchAsync(int jobId, CancellationToken cancellationToken = default)
    {
        if (!DemoContract.ContainingTerritoryByJobId.TryGetValue(jobId, out int territoryId))
        {
            throw new DispatchUnavailableException(
                $"Job {jobId} is not covered by any service territory.");
        }

        Territory territory = DemoContract.TerritoryById(territoryId);

        // Rank first, then label. The winner is whichever eligible technician sorts first by distance, so the
        // outcome shown next to each name is derived from the query rather than asserted alongside it.
        List<DemoContract.DemoTechnician> eligible = DemoContract.Technicians
            .Where(t => t.IsAvailable && t.TerritoryId == territory.Id)
            .OrderBy(t => DemoContract.ReferenceDistance(jobId, t.Id))
            .ToList();

        if (eligible.Count == 0)
        {
            throw new DispatchUnavailableException(
                $"No available technician is assigned to {territory.Name}.");
        }

        DemoContract.DemoTechnician winner = eligible[0];

        List<TechnicianCandidate> candidates = DemoContract.Technicians
            .Select(t => ToCandidate(t, jobId, territory, winner.Id))
            .OrderBy(c => c.DistanceMeters)
            .ToList();

        TechnicianCandidate recommended = candidates.Single(c => c.Id == winner.Id);

        return Task.FromResult(new DispatchResult(
            jobId,
            territory,
            recommended,
            recommended.DistanceMeters,
            candidates));
    }

    private static TechnicianCandidate ToCandidate(
        DemoContract.DemoTechnician technician,
        int jobId,
        Territory containingTerritory,
        int winnerId)
    {
        CandidateOutcome outcome = technician switch
        {
            { IsAvailable: false } => CandidateOutcome.ExcludedByAvailability,
            _ when technician.TerritoryId != containingTerritory.Id => CandidateOutcome.ExcludedByTerritory,
            _ when technician.Id == winnerId => CandidateOutcome.Recommended,
            _ => CandidateOutcome.Eligible
        };

        return new TechnicianCandidate(
            technician.Id,
            technician.Name,
            technician.TerritoryId,
            DemoContract.TerritoryById(technician.TerritoryId).Name,
            technician.IsAvailable,
            technician.LocationLabel,
            technician.Location,
            DemoContract.ReferenceDistance(jobId, technician.Id),
            outcome);
    }
}
