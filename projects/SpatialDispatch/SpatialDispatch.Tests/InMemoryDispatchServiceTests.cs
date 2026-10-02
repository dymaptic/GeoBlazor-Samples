using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Tests;

/// <summary>
///     Holds the in-memory provider to the acceptance checks in <c>docs/demo-contract.md</c>.
/// </summary>
/// <remarks>
///     Task 3 replaces this provider with SQL Server spatial queries. These assertions move to the
///     integration suite unchanged, which is the point: the document, not the implementation, defines what a
///     correct answer looks like.
/// </remarks>
public class InMemoryDispatchServiceTests
{
    private const int JobId = DemoContract.CanonicalJobId;

    private readonly InMemoryDispatchService _service = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Open_jobs_include_the_canonical_urgent_job()
    {
        IReadOnlyList<Job> jobs = await _service.GetOpenJobsAsync(Ct);

        Job job = Assert.Single(jobs, j => j.Id == JobId);
        Assert.Equal("Summit Sports Catering", job.Customer);
        Assert.Equal(JobPriority.Urgent, job.Priority);
        Assert.Equal(-75.683967, job.Location.Longitude, 6);
        Assert.Equal(41.360406, job.Location.Latitude, 6);
    }

    [Fact]
    public async Task Canonical_job_is_contained_by_exactly_one_territory()
    {
        DispatchResult result = await _service.GetDispatchAsync(JobId, Ct);

        Assert.Equal(DemoContract.NorthValleyId, result.ContainingTerritory.Id);
        Assert.Equal("North Valley", result.ContainingTerritory.Name);
    }

    [Fact]
    public async Task Nearest_eligible_technician_is_recommended()
    {
        DispatchResult result = await _service.GetDispatchAsync(JobId, Ct);

        Assert.Equal(DemoContract.RecommendedTechnicianId, result.RecommendedTechnician.Id);
        Assert.Equal("Jordan Alvarez", result.RecommendedTechnician.Name);
        Assert.Equal(CandidateOutcome.Recommended, result.RecommendedTechnician.Outcome);
    }

    [Fact]
    public async Task Straight_line_distance_matches_the_documented_reference_value()
    {
        DispatchResult result = await _service.GetDispatchAsync(JobId, Ct);

        // The demo contract allows 20 m of slack because SQL Server documents STDistance as an
        // approximation within 0.25 percent, and Task 3 has to satisfy the same assertion.
        Assert.Equal(5_688d, result.StraightLineDistanceMeters, tolerance: 20d);
    }

    [Theory]
    [InlineData(201, CandidateOutcome.ExcludedByAvailability)]
    [InlineData(202, CandidateOutcome.ExcludedByTerritory)]
    [InlineData(203, CandidateOutcome.Recommended)]
    [InlineData(204, CandidateOutcome.Eligible)]
    [InlineData(205, CandidateOutcome.Eligible)]
    [InlineData(206, CandidateOutcome.Eligible)]
    [InlineData(207, CandidateOutcome.ExcludedByTerritory)]
    [InlineData(208, CandidateOutcome.ExcludedByTerritory)]
    [InlineData(209, CandidateOutcome.ExcludedByAvailability)]
    public async Task Every_candidate_carries_the_reason_the_query_kept_or_dropped_it(
        int technicianId,
        CandidateOutcome expected)
    {
        DispatchResult result = await _service.GetDispatchAsync(JobId, Ct);

        TechnicianCandidate candidate = Assert.Single(result.Candidates, c => c.Id == technicianId);
        Assert.Equal(expected, candidate.Outcome);
    }

    [Fact]
    public async Task Closer_technicians_lose_to_the_recommendation_on_availability_and_territory()
    {
        DispatchResult result = await _service.GetDispatchAsync(JobId, Ct);

        double recommended = result.RecommendedTechnician.DistanceMeters;
        TechnicianCandidate morgan = result.Candidates.Single(c => c.Id == 201);
        TechnicianCandidate riley = result.Candidates.Single(c => c.Id == 202);

        // The cold-open question only lands if the audience can see the query rejecting people who are
        // plainly nearer. If either of these ever sorts behind the winner, the demo stops proving anything.
        Assert.True(morgan.DistanceMeters < recommended);
        Assert.False(morgan.IsAvailable);

        Assert.True(riley.DistanceMeters < recommended);
        Assert.True(riley.IsAvailable);
        Assert.NotEqual(result.ContainingTerritory.Id, riley.TerritoryId);
    }

    [Fact]
    public async Task Candidates_are_listed_nearest_first()
    {
        DispatchResult result = await _service.GetDispatchAsync(JobId, Ct);

        double[] distances = result.Candidates.Select(c => c.DistanceMeters).ToArray();
        Assert.Equal(distances.OrderBy(d => d), distances);
    }

    [Theory]
    [InlineData(1001, DemoContract.NorthValleyId, 203, 5_688d)]
    [InlineData(1002, DemoContract.SouthValleyId, 207, 2_364d)]
    [InlineData(1003, DemoContract.NorthValleyId, 203, 1_522d)]
    public async Task Every_job_gets_the_territory_and_technician_the_contract_records(
        int jobId,
        int territoryId,
        int technicianId,
        double meters)
    {
        DispatchResult result = await _service.GetDispatchAsync(jobId, Ct);

        Assert.Equal(territoryId, result.ContainingTerritory.Id);
        Assert.Equal(technicianId, result.RecommendedTechnician.Id);
        Assert.Equal(meters, result.StraightLineDistanceMeters, tolerance: 20d);
    }

    [Fact]
    public async Task Unknown_job_fails_rather_than_inventing_a_territory()
    {
        await Assert.ThrowsAsync<DispatchUnavailableException>(() => _service.GetDispatchAsync(9999, Ct));
    }
}
