using Microsoft.EntityFrameworkCore;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;
using JobEntity = SpatialDispatch.Data.Entities.Job;
using TerritoryEntity = SpatialDispatch.Data.Entities.Territory;

namespace SpatialDispatch.Data;

/// <summary>
///     Answers the dispatcher's question with two spatial queries that run inside SQL Server.
/// </summary>
/// <remarks>
///     Nothing here pulls a table into memory to measure distances. Containment becomes <c>STContains</c>
///     and distance becomes <c>STDistance</c>, and both compose with ordinary <c>Where</c> clauses over
///     ordinary business columns. That composition is the session's entire claim.
/// </remarks>
public sealed class SqlServerDispatchService(DispatchDbContext db) : IDispatchService
{
    public async Task<IReadOnlyList<Job>> GetOpenJobsAsync(CancellationToken cancellationToken = default)
    {
        List<JobEntity> jobs = await db.Jobs.ToListAsync(cancellationToken);

        // Priority is stored as text so the table is readable in SQL Server Management Studio during the
        // demo, which means ordering by it in SQL would sort alphabetically. Ordering the materialized list
        // sorts by the enum instead, putting Urgent ahead of Routine for the reason it looks like it does.
        return jobs
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.Id)
            .Select(j => new Job(
                j.Id,
                j.Customer,
                j.Summary,
                j.Priority,
                j.MapAnchor,
                new GeoPoint(j.Location.X, j.Location.Y)))
            .ToList();
    }

    public async Task<DispatchResult> GetDispatchAsync(
        int jobId,
        CancellationToken cancellationToken = default)
    {
        JobEntity job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
            ?? throw new DispatchUnavailableException($"Job {jobId} does not exist.");

        // Query one: which territory covers this point? The predicate translates to geography::STContains.
        // Materialize every match before enforcing uniqueness. SingleOrDefaultAsync emits TOP(2), whose row
        // goal makes SQL Server prefer a scan instead of the territory spatial index.
        List<TerritoryEntity> containingTerritories = await db.Territories
            .Where(t => t.Boundary.Contains(job.Location))
            .ToListAsync(cancellationToken);
        TerritoryEntity? territory = containingTerritories.SingleOrDefault();

        if (territory is null)
        {
            throw new DispatchUnavailableException(
                $"Job {jobId} is not covered by any service territory.");
        }

        // Query two: of the available technicians assigned to that territory, who is closest? Availability
        // and territory are plain business filters; only the ordering is spatial, and on geography the
        // distance comes back in meters.
        int? winnerId = await db.Technicians
            .Where(t =>
                t.Location.Distance(job.Location) <= DemoContract.CompanyExtentDiagonalMeters &&
                t.TerritoryId == territory.Id &&
                t.IsAvailable)
            .OrderBy(t => t.Location.Distance(job.Location))
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (winnerId is null)
        {
            throw new DispatchUnavailableException(
                $"No available technician is assigned to {territory.Name}.");
        }

        // The candidate list exists so the audience can watch the filters reject people who are plainly
        // nearer. It is not needed to make the assignment, and it never overrides the query above.
        List<CandidateRow> rows = await db.Technicians
            .OrderBy(t => t.Location.Distance(job.Location))
            .Select(t => new CandidateRow(
                t.Id,
                t.Name,
                t.TerritoryId,
                t.Territory!.Name,
                t.IsAvailable,
                t.LocationLabel,
                t.Location.X,
                t.Location.Y,
                t.Location.Distance(job.Location)))
            .ToListAsync(cancellationToken);

        List<TechnicianCandidate> candidates = rows
            .Select(r => ToCandidate(r, territory.Id, winnerId.Value))
            .ToList();

        TechnicianCandidate recommended = candidates.Single(c => c.Id == winnerId.Value);

        return new DispatchResult(
            jobId,
            new Territory(territory.Id, territory.Name),
            recommended,
            recommended.DistanceMeters,
            candidates);
    }

    private static TechnicianCandidate ToCandidate(CandidateRow row, int containingTerritoryId, int winnerId)
    {
        CandidateOutcome outcome = row switch
        {
            { IsAvailable: false } => CandidateOutcome.ExcludedByAvailability,
            _ when row.TerritoryId != containingTerritoryId => CandidateOutcome.ExcludedByTerritory,
            _ when row.Id == winnerId => CandidateOutcome.Recommended,
            _ => CandidateOutcome.Eligible
        };

        return new TechnicianCandidate(
            row.Id,
            row.Name,
            row.TerritoryId,
            row.TerritoryName,
            row.IsAvailable,
            row.LocationLabel,
            new GeoPoint(row.Longitude, row.Latitude),
            row.DistanceMeters,
            outcome);
    }

    /// <summary>One technician as the database ranked them, before the outcome label is applied.</summary>
    private sealed record CandidateRow(
        int Id,
        string Name,
        int TerritoryId,
        string TerritoryName,
        bool IsAvailable,
        string LocationLabel,
        double Longitude,
        double Latitude,
        double DistanceMeters);
}
