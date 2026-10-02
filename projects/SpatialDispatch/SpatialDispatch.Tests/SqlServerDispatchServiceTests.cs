using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SpatialDispatch.Data;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Tests;

/// <summary>
///     Holds the SQL Server implementation to the same acceptance checks in <c>docs/demo-contract.md</c>
///     that the in-memory provider satisfies.
/// </summary>
/// <remarks>
///     These assertions are deliberately near-identical to <see cref="InMemoryDispatchServiceTests" />. The
///     document defines what a correct answer looks like; swapping canned data for translated spatial
///     queries must not change it. Where the two differ, it is only in tolerance: the in-memory provider
///     replays recorded distances, while these come from <c>geography::STDistance</c>, which SQL Server
///     documents as accurate to within 0.25 percent.
/// </remarks>
[Collection(SqlServerCollection.Name)]
public class SqlServerDispatchServiceTests(SqlServerDispatchFixture fixture)
{
    private const int JobId = DemoContract.CanonicalJobId;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Open_jobs_come_back_from_the_database()
    {
        IDispatchService service = RequireService();

        IReadOnlyList<Job> jobs = await service.GetOpenJobsAsync(Ct);

        Job job = Assert.Single(jobs, j => j.Id == JobId);
        Assert.Equal("Summit Sports Catering", job.Customer);
        Assert.Equal(JobPriority.Urgent, job.Priority);

        // Round-tripping through a geography column must not swap the pair. If it ever does, the marker
        // lands in East Antarctica and the trap the talk warns about becomes the talk's own bug.
        Assert.Equal(-75.683967, job.Location.Longitude, 6);
        Assert.Equal(41.360406, job.Location.Latitude, 6);
    }

    [Fact]
    public async Task Containment_finds_exactly_one_territory()
    {
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        Assert.Equal(DemoContract.NorthValleyId, result.ContainingTerritory.Id);
        Assert.Equal("North Valley", result.ContainingTerritory.Name);
    }

    [Fact]
    public async Task Containment_query_has_no_row_goal()
    {
        IReadOnlyList<string> commands = await CaptureDispatchCommandsAsync();

        string containmentQuery = Assert.Single(
            commands,
            command =>
                command.Contains("FROM [Territories] AS [t]", StringComparison.Ordinal) &&
                command.Contains("STContains", StringComparison.Ordinal));

        Assert.DoesNotContain("TOP(2)", containmentQuery, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Only_the_northern_territory_contains_the_job()
    {
        SkipIfUnavailable();

        // A geography polygon whose exterior ring is wound the wrong way is not rejected: SQL Server takes
        // it as the complement, the whole globe minus the intended shape. Containment would then be true for
        // both territories, so asserting that South Valley excludes the job is what proves the winding.
        List<int> containing = await fixture.Db.Territories
            .Where(t => t.Boundary.Contains(fixture.Db.Jobs.Single(j => j.Id == JobId).Location))
            .Select(t => t.Id)
            .ToListAsync(Ct);

        Assert.Equal([DemoContract.NorthValleyId], containing);
    }

    [Fact]
    public async Task Nearest_eligible_technician_is_recommended()
    {
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        Assert.Equal(DemoContract.RecommendedTechnicianId, result.RecommendedTechnician.Id);
        Assert.Equal("Jordan Alvarez", result.RecommendedTechnician.Name);
        Assert.Equal(CandidateOutcome.Recommended, result.RecommendedTechnician.Outcome);
    }

    [Fact]
    public async Task Winner_query_bounds_distance_by_the_company_extent()
    {
        IReadOnlyList<string> commands = await CaptureDispatchCommandsAsync();

        string winnerQuery = Assert.Single(
            commands,
            command =>
                command.Contains("SELECT TOP(1)", StringComparison.Ordinal) &&
                command.Contains("FROM [Technicians] AS [t]", StringComparison.Ordinal));
        int whereStart = winnerQuery.IndexOf("WHERE", StringComparison.Ordinal);
        int orderByStart = winnerQuery.IndexOf("ORDER BY", StringComparison.Ordinal);
        string predicate = winnerQuery[whereStart..orderByStart];

        Assert.Contains("[t].[Location].STDistance(", predicate, StringComparison.Ordinal);
        Assert.Contains("<=", predicate, StringComparison.Ordinal);
        Assert.Contains("45269", predicate, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Distance_matches_the_documented_reference_value()
    {
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        Assert.Equal(5_688d, result.StraightLineDistanceMeters, tolerance: 20d);
    }

    [Theory]
    [InlineData(201)]
    [InlineData(202)]
    [InlineData(203)]
    [InlineData(204)]
    [InlineData(205)]
    [InlineData(206)]
    [InlineData(207)]
    [InlineData(208)]
    [InlineData(209)]
    public async Task Every_measured_distance_matches_the_demo_contract(int technicianId)
    {
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        TechnicianCandidate candidate = Assert.Single(result.Candidates, c => c.Id == technicianId);
        Assert.Equal(
            DemoContract.ReferenceDistance(JobId, technicianId),
            candidate.DistanceMeters,
            tolerance: 20d);
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
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        TechnicianCandidate candidate = Assert.Single(result.Candidates, c => c.Id == technicianId);
        Assert.Equal(expected, candidate.Outcome);
    }

    [Fact]
    public async Task Closer_technicians_lose_on_availability_and_territory()
    {
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        double recommended = result.RecommendedTechnician.DistanceMeters;
        TechnicianCandidate morgan = result.Candidates.Single(c => c.Id == 201);
        TechnicianCandidate riley = result.Candidates.Single(c => c.Id == 202);

        Assert.True(morgan.DistanceMeters < recommended);
        Assert.False(morgan.IsAvailable);

        Assert.True(riley.DistanceMeters < recommended);
        Assert.True(riley.IsAvailable);
        Assert.NotEqual(result.ContainingTerritory.Id, riley.TerritoryId);
    }

    [Fact]
    public async Task Candidates_are_listed_nearest_first()
    {
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(JobId, Ct);

        double[] distances = result.Candidates.Select(c => c.DistanceMeters).ToArray();
        Assert.Equal(distances.OrderBy(d => d), distances);
    }

    [Fact]
    public async Task Unknown_job_fails_rather_than_inventing_a_territory()
    {
        IDispatchService service = RequireService();

        await Assert.ThrowsAsync<DispatchUnavailableException>(() => service.GetDispatchAsync(9999, Ct));
    }

    /// <remarks>
    ///     The two routine jobs are here because one selection has to land in each territory. A containment
    ///     query that always answers North Valley would pass every other test in this class.
    /// </remarks>
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
        IDispatchService service = RequireService();

        DispatchResult result = await service.GetDispatchAsync(jobId, Ct);

        Assert.Equal(territoryId, result.ContainingTerritory.Id);
        Assert.Equal(technicianId, result.RecommendedTechnician.Id);
        Assert.Equal(meters, result.StraightLineDistanceMeters, tolerance: 20d);
    }

    private IDispatchService RequireService()
    {
        SkipIfUnavailable();

        return new SqlServerDispatchService(fixture.Db);
    }

    private async Task<IReadOnlyList<string>> CaptureDispatchCommandsAsync()
    {
        SkipIfUnavailable();

        CommandRecorder recorder = new();
        DbContextOptions<DispatchDbContext> options = new DbContextOptionsBuilder<DispatchDbContext>()
            .UseSqlServer(
                fixture.Db.Database.GetConnectionString(),
                sqlServer => sqlServer.UseNetTopologySuite())
            .AddInterceptors(recorder)
            .Options;

        await using DispatchDbContext db = new(options);
        await new SqlServerDispatchService(db).GetDispatchAsync(JobId, Ct);

        return recorder.Commands;
    }

    private void SkipIfUnavailable()
    {
        Assert.SkipUnless(fixture.IsAvailable, SqlServerDispatchFixture.SkipReason);
    }

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
