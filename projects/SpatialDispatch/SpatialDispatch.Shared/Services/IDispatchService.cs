using SpatialDispatch.Shared.Contracts;

namespace SpatialDispatch.Shared.Services;

/// <summary>
///     The dispatch workflow the dashboard consumes.
/// </summary>
/// <remarks>
///     Task 2 satisfies this from deterministic in-memory data taken from <c>docs/demo-contract.md</c>.
///     Task 3 replaces the implementation with one that calls the Minimal API over SQL Server spatial
///     queries. The dashboard does not change when that swap happens.
/// </remarks>
public interface IDispatchService
{
    Task<IReadOnlyList<Job>> GetOpenJobsAsync(CancellationToken cancellationToken = default);

    Task<DispatchResult> GetDispatchAsync(int jobId, CancellationToken cancellationToken = default);
}
