using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Shared.Api;

/// <summary>
///     The dashboard's dispatch provider once a real database is behind the application: an HTTP call to the
///     Minimal API, which runs the spatial queries.
/// </summary>
/// <remarks>
///     This is the whole of the Task 3 swap on the client side. The dashboard consumes
///     <see cref="IDispatchService" /> and cannot tell that the answer now comes from SQL Server, which is
///     the point of having put the interface there in the first place.
/// </remarks>
public sealed class ApiDispatchService(HttpClient http) : IDispatchService
{
    public async Task<IReadOnlyList<Job>> GetOpenJobsAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<List<Job>>("api/jobs", cancellationToken)
        ?? throw new InvalidOperationException("The open jobs endpoint returned no content.");

    public async Task<DispatchResult> GetDispatchAsync(
        int jobId,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await http.GetAsync($"api/dispatch/{jobId}", cancellationToken);

        // A job outside every territory comes back as a problem response, not a transport failure. The
        // dashboard shows its message, so unwrap it rather than surfacing "404 Not Found" to the room.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                await ReadProblemDetailAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DispatchResult>(cancellationToken)
            ?? throw new InvalidOperationException($"The dispatch endpoint returned no content for job {jobId}.");
    }

    private static async Task<string> ReadProblemDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument problem = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            if (problem.RootElement.TryGetProperty("detail", out JsonElement detail)
                && detail.GetString() is { Length: > 0 } message)
            {
                return message;
            }
        }
        catch (JsonException)
        {
            // Fall through to the generic message below.
        }

        return "No dispatch answer is available for this job.";
    }
}
