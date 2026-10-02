using System.Net;
using System.Text;
using System.Text.Json;
using SpatialDispatch.Shared.Api;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;

namespace SpatialDispatch.Tests;

/// <summary>
///     Covers the HTTP provider's translation of every response the API can send back: an ordinary answer, a
///     problem response carrying a readable detail, a not-found that is not JSON at all, a server fault, and
///     an empty body.
/// </summary>
/// <remarks>
///     The provider is the boundary the dashboard cannot see past, so its messages are what the room reads
///     when the API says no. Driving it with a stubbed <see cref="HttpMessageHandler" /> rather than a live
///     server keeps these tests in the cold-clone green set.
/// </remarks>
public class ApiDispatchServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Open_jobs_are_read_from_the_jobs_endpoint()
    {
        Job job = new(
            1001,
            "Summit Sports Catering",
            "Walk-in freezer temperature alarm",
            JobPriority.Urgent,
            "PNC Field, Moosic, Pennsylvania",
            new GeoPoint(-75.683967, 41.360406));

        ApiDispatchService service = ServiceReturning(HttpStatusCode.OK, new[] { job });

        IReadOnlyList<Job> jobs = await service.GetOpenJobsAsync(Ct);

        Job returned = Assert.Single(jobs);
        Assert.Equal(job, returned);
    }

    [Fact]
    public async Task A_dispatch_answer_is_read_from_the_api()
    {
        ApiDispatchService service = ServiceReturning(HttpStatusCode.OK, DispatchAnswer());

        DispatchResult result = await service.GetDispatchAsync(DemoContract.CanonicalJobId, Ct);

        Assert.Equal("North Valley", result.ContainingTerritory.Name);
        Assert.Equal("Jordan Alvarez", result.RecommendedTechnician.Name);
        Assert.Equal(5_688d, result.StraightLineDistanceMeters);
    }

    [Fact]
    public async Task A_problem_response_reads_its_detail_to_the_room()
    {
        ApiDispatchService service = ServiceReturningRaw(
            HttpStatusCode.NotFound,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"detail":"Job 9999 is not covered by any service territory."}""",
            "application/problem+json");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDispatchAsync(9999, Ct));

        Assert.Equal("Job 9999 is not covered by any service territory.", exception.Message);
    }

    [Fact]
    public async Task A_not_found_that_is_not_a_problem_response_gets_a_generic_message()
    {
        ApiDispatchService service = ServiceReturningRaw(
            HttpStatusCode.NotFound,
            "<html><body>Not found</body></html>",
            "text/html");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDispatchAsync(9999, Ct));

        Assert.Equal("No dispatch answer is available for this job.", exception.Message);
    }

    [Fact]
    public async Task A_server_fault_is_not_disguised_as_a_missing_answer()
    {
        ApiDispatchService service = ServiceReturningRaw(
            HttpStatusCode.InternalServerError,
            "boom",
            "text/plain");

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetDispatchAsync(1001, Ct));
    }

    [Fact]
    public async Task An_empty_body_is_an_error_rather_than_a_blank_card()
    {
        ApiDispatchService service = ServiceReturningRaw(HttpStatusCode.OK, string.Empty);

        await Assert.ThrowsAsync<JsonException>(() => service.GetDispatchAsync(1001, Ct));
    }

    private static DispatchResult DispatchAnswer() =>
        new(
            DemoContract.CanonicalJobId,
            new Territory(DemoContract.NorthValleyId, "North Valley"),
            new TechnicianCandidate(
                DemoContract.RecommendedTechnicianId,
                "Jordan Alvarez",
                DemoContract.NorthValleyId,
                "North Valley",
                IsAvailable: true,
                "Steamtown service stop",
                new GeoPoint(-75.671329, 41.410730),
                5_688d,
                CandidateOutcome.Recommended),
            5_688d,
            [
                new TechnicianCandidate(
                    DemoContract.RecommendedTechnicianId,
                    "Jordan Alvarez",
                    DemoContract.NorthValleyId,
                    "North Valley",
                    IsAvailable: true,
                    "Steamtown service stop",
                    new GeoPoint(-75.671329, 41.410730),
                    5_688d,
                    CandidateOutcome.Recommended)
            ]);

    private static ApiDispatchService ServiceReturning<T>(HttpStatusCode status, T value) =>
        ServiceReturningRaw(status, JsonSerializer.Serialize(value));

    private static ApiDispatchService ServiceReturningRaw(
        HttpStatusCode status,
        string body,
        string mediaType = "application/json") =>
        new(new HttpClient(new StubHttpMessageHandler(status, body, mediaType))
        {
            BaseAddress = new Uri("https://spatialdispatch.test/")
        });

    /// <summary>
    ///     Answers every request with the same canned response, so the test controls the protocol outcome
    ///     without a network or a server.
    /// </summary>
    private sealed class StubHttpMessageHandler(
        HttpStatusCode status,
        string body,
        string mediaType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(status)
            {
                Content = new StringContent(body, Encoding.UTF8, mediaType)
            };

            return Task.FromResult(response);
        }
    }
}
