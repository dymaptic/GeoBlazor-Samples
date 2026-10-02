using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpatialDispatch.Client.Components;
using SpatialDispatch.Client.Pages;
using SpatialDispatch.Shared.Contracts;
using SpatialDispatch.Shared.Demo;
using SpatialDispatch.Shared.Services;

namespace SpatialDispatch.Tests;

/// <summary>
///     Behavior tests for the audience-visible workflow.
/// </summary>
/// <remarks>
///     <see cref="DispatchMap" /> is stubbed out. bUnit cannot run the ArcGIS JavaScript interop the map
///     depends on, and these tests are about what the dispatcher sees and clicks, not about the map surface.
///     Isolating GeoBlazor in one child component is what makes that substitution possible.
/// </remarks>
public class DispatchBoardTests : BunitContext
{
    private const int JobId = DemoContract.CanonicalJobId;

    private readonly IDispatchService _dispatchService = Substitute.For<IDispatchService>();
    private readonly IRouteService _routeService = Substitute.For<IRouteService>();

    public DispatchBoardTests()
    {
        ComponentFactories.AddStub<DispatchMap>();
        Services.AddSingleton(_dispatchService);
        Services.AddSingleton(_routeService);
    }

    [Fact]
    public void No_job_selected_prompts_the_dispatcher_to_pick_one()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();

        Assert.NotNull(board.Find("[data-testid=no-selection]"));
        Assert.Empty(board.FindAll("[data-testid=recommendation]"));
    }

    [Fact]
    public void Empty_job_list_says_so_instead_of_rendering_nothing()
    {
        _dispatchService.GetOpenJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Job>>([]));

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();

        Assert.NotNull(board.Find("[data-testid=jobs-empty]"));
    }

    [Fact]
    public void Failure_to_load_jobs_shows_an_error_rather_than_an_empty_board()
    {
        _dispatchService.GetOpenJobsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("database unreachable"));

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();

        Assert.Contains("database unreachable", board.Find("[data-testid=jobs-error]").TextContent);
    }

    [Fact]
    public void Selecting_the_job_shows_the_territory_technician_and_distance()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        Assert.Equal("North Valley", board.Find("[data-testid=containing-territory]").TextContent.Trim());
        Assert.Equal("Jordan Alvarez", board.Find("[data-testid=recommended-technician]").TextContent.Trim());
        Assert.Equal("5.7 km", board.Find("[data-testid=recommended-distance]").TextContent.Trim());
    }

    [Fact]
    public void Candidate_list_shows_why_nearer_technicians_lost()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        Assert.Equal("Unavailable", OutcomeText(board, 201));
        Assert.Equal("Assigned to South Valley", OutcomeText(board, 202));
        Assert.Equal("Recommended", OutcomeText(board, 203));
        Assert.Equal("Eligible - North Valley", OutcomeText(board, 204));
    }

    [Fact]
    public void Dispatch_failure_leaves_the_board_usable()
    {
        UseRealDemoData();
        _dispatchService.GetDispatchAsync(JobId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("no covering territory"));

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        Assert.Contains("no covering territory", board.Find("[data-testid=dispatch-error]").TextContent);
        Assert.Empty(board.FindAll("[data-testid=assign-button]"));
    }

    [Fact]
    public void Straight_line_fallback_is_labeled_and_does_not_change_the_recommendation()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        Assert.Contains("straight connector", board.Find("[data-testid=route-note]").TextContent);
        Assert.Equal("Jordan Alvarez", board.Find("[data-testid=recommended-technician]").TextContent.Trim());
    }

    [Fact]
    public void A_routing_failure_still_draws_a_connector()
    {
        UseRealDemoData();
        _routeService
            .GetRouteAsync(Arg.Any<GeoPoint>(), Arg.Any<GeoPoint>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("routing service unreachable"));

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        Assert.Contains("straight connector", board.Find("[data-testid=route-note]").TextContent);
    }

    [Fact]
    public void Assigning_moves_the_job_to_assigned_and_names_the_technician()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        Assert.Equal("Assign Jordan", board.Find("[data-testid=assign-button]").TextContent.Trim());
        board.Find("[data-testid=assign-button]").Click();

        Assert.Equal("Assigned", board.Find($"[data-testid=job-status-{JobId}]").TextContent.Trim());
        Assert.Equal("Jordan Alvarez", board.Find($"[data-testid=job-assignee-{JobId}]").TextContent.Trim());
        Assert.Contains("Jordan Alvarez", board.Find("[data-testid=assigned-note]").TextContent);
        Assert.Empty(board.FindAll("[data-testid=assign-button]"));
    }

    [Fact]
    public void Refreshing_the_page_restores_the_job_to_open()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> first = Render<DispatchBoard>();
        SelectCanonicalJob(first);
        first.Find("[data-testid=assign-button]").Click();
        Assert.Equal("Assigned", first.Find($"[data-testid=job-status-{JobId}]").TextContent.Trim());

        // A refresh is a new component instance over the same services. Assignment is presentation state
        // held in the component, so nothing survives. bUnit's JS interop runs in strict mode by default, so
        // this test would also fail if the board ever started writing the assignment to browser storage.
        IRenderedComponent<DispatchBoard> afterRefresh = Render<DispatchBoard>();

        Assert.Equal("Open", afterRefresh.Find($"[data-testid=job-status-{JobId}]").TextContent.Trim());
        Assert.Empty(afterRefresh.FindAll($"[data-testid=job-assignee-{JobId}]"));
        Assert.NotNull(afterRefresh.Find("[data-testid=no-selection]"));
    }

    [Fact]
    public void Selecting_a_candidate_previews_that_candidates_route_without_changing_the_recommendation()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        GeoPoint jordan = new(-75.671329, 41.410730);
        GeoPoint casey = new(-75.625401, 41.465984);
        GeoPoint job = new(-75.683967, 41.360406);

        _routeService.Received().GetRouteAsync(jordan, job, Arg.Any<CancellationToken>());

        board.Find("[data-testid=candidate-204]").Click();

        _routeService.Received().GetRouteAsync(casey, job, Arg.Any<CancellationToken>());
        Assert.Equal("Jordan Alvarez", board.Find("[data-testid=recommended-technician]").TextContent.Trim());
    }

    [Fact]
    public void Selecting_a_candidate_marks_its_row_selected()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);

        board.Find("[data-testid=candidate-204]").Click();

        Assert.Contains("selected", board.Find("[data-testid=candidate-204]").ClassList);
        Assert.Equal("true", board.Find("[data-testid=candidate-204]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Choosing_the_job_again_clears_the_candidate_preview()
    {
        UseRealDemoData();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();
        SelectCanonicalJob(board);
        board.Find("[data-testid=candidate-204]").Click();
        Assert.Contains("selected", board.Find("[data-testid=candidate-204]").ClassList);

        SelectCanonicalJob(board);

        Assert.DoesNotContain("selected", board.Find("[data-testid=candidate-204]").ClassList);
    }

    [Fact]
    public void A_dispatch_answer_for_an_abandoned_job_does_not_replace_the_current_answer()
    {
        Job first = new(
            101, "First customer", "First job", JobPriority.Routine, "First anchor",
            new GeoPoint(-75.700000, 41.360000));
        Job second = new(
            102, "Second customer", "Second job", JobPriority.Routine, "Second anchor",
            new GeoPoint(-75.680000, 41.350000));

        _dispatchService.GetOpenJobsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Job>>([first, second]));

        TaskCompletionSource<DispatchResult> firstAnswer = new();
        TaskCompletionSource<DispatchResult> secondAnswer = new();

        _dispatchService.GetDispatchAsync(101, Arg.Any<CancellationToken>()).Returns(firstAnswer.Task);
        _dispatchService.GetDispatchAsync(102, Arg.Any<CancellationToken>()).Returns(secondAnswer.Task);
        UseStraightLineRoutes();

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();

        // Both clicks are dispatched without waiting, so the first job's answer is still in flight when
        // the dispatcher picks the second job.
        _ = board.Find("[data-testid=job-101]").TriggerEventAsync("onclick", new MouseEventArgs());
        _ = board.Find("[data-testid=job-102]").TriggerEventAsync("onclick", new MouseEventArgs());

        secondAnswer.SetResult(AnswerFor(second.Id, "Second technician"));
        firstAnswer.SetResult(AnswerFor(first.Id, "First technician"));

        // The abandoned job's slower answer must be discarded, not shown over the current selection.
        board.WaitForAssertion(() =>
            Assert.Equal(
                "Second technician",
                board.Find("[data-testid=recommended-technician]").TextContent.Trim()));
    }

    [Fact]
    public void A_route_that_resolves_after_a_candidate_preview_does_not_replace_the_preview()
    {
        UseRealDispatchData();

        GeoPoint jordanLocation = new(-75.671329, 41.410730);
        GeoPoint caseyLocation = new(-75.625401, 41.465984);
        GeoPoint jobLocation = new(-75.683967, 41.360406);

        TaskCompletionSource<RoutePath> recommendedRoute = new();

        _routeService.GetRouteAsync(jordanLocation, jobLocation, Arg.Any<CancellationToken>())
            .Returns(recommendedRoute.Task);
        _routeService.GetRouteAsync(caseyLocation, jobLocation, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RoutePath([caseyLocation, jobLocation], IsFallback: false)));

        IRenderedComponent<DispatchBoard> board = Render<DispatchBoard>();

        _ = board.Find($"[data-testid=job-{JobId}]").TriggerEventAsync("onclick", new MouseEventArgs());
        board.WaitForElement("[data-testid=candidate-204]");

        // The recommendation's route is still pending; previewing a candidate draws that route at once.
        _ = board.Find("[data-testid=candidate-204]").TriggerEventAsync("onclick", new MouseEventArgs());

        // The slow route for the recommendation arrives last and must not replace the preview's road route.
        recommendedRoute.SetResult(new RoutePath([jordanLocation, jobLocation], IsFallback: true));

        board.WaitForAssertion(() =>
            Assert.Contains("Road route drawn", board.Find("[data-testid=route-note]").TextContent));
    }

    /// <summary>
    ///     Points the substitutes at the real demo-contract provider, so the tests exercise the same records
    ///     the presentation uses rather than a parallel set of fixtures that could drift from the document.
    /// </summary>
    private void UseRealDemoData()
    {
        UseRealDispatchData();
        UseStraightLineRoutes();
    }

    private void UseRealDispatchData()
    {
        InMemoryDispatchService real = new();

        _dispatchService.GetOpenJobsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => real.GetOpenJobsAsync());
        _dispatchService.GetDispatchAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => real.GetDispatchAsync(call.Arg<int>()));
    }

    private void UseStraightLineRoutes()
    {
        _routeService
            .GetRouteAsync(Arg.Any<GeoPoint>(), Arg.Any<GeoPoint>(), Arg.Any<CancellationToken>())
            .Returns(call => new StraightLineRouteService()
                .GetRouteAsync(call.ArgAt<GeoPoint>(0), call.ArgAt<GeoPoint>(1)));
    }

    private static DispatchResult AnswerFor(int jobId, string technicianName) =>
        new(
            jobId,
            new Territory(1, "North Valley"),
            CandidateFor(jobId, technicianName),
            100d,
            [CandidateFor(jobId, technicianName)]);

    private static TechnicianCandidate CandidateFor(int jobId, string technicianName) =>
        new(
            900 + jobId,
            technicianName,
            1,
            "North Valley",
            IsAvailable: true,
            "Somewhere service stop",
            new GeoPoint(-75.700000, 41.360000),
            100d,
            CandidateOutcome.Recommended);

    private static void SelectCanonicalJob(IRenderedComponent<DispatchBoard> board) =>
        board.Find($"[data-testid=job-{JobId}]").Click();

    private static string OutcomeText(IRenderedComponent<DispatchBoard> board, int technicianId) =>
        board.Find($"[data-testid=candidate-outcome-{technicianId}]").TextContent.Trim();
}
