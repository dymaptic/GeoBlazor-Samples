using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class ScenarioSettingsTests
{
    // Length caps that keep the presenter text readable in the Mode picker and the guidance panel.
    private const int MaxDisplayNameLength = 48;
    private const int MaxWatchingLineLength = 135;
    private const int MaxDemonstrationCueLength = 150;

    [TestMethod]
    public void TheNormalPresetCarriesFiftyVehiclesAtTwoReportsAndTwoFramesPerSecond()
    {
        Assert.AreEqual(50, ScenarioSettings.Normal.VehicleCount);
        Assert.AreEqual(2, ScenarioSettings.Normal.ReportsPerSecond);
        Assert.AreEqual(2, ScenarioSettings.Normal.FramesPerSecond);
        Assert.AreEqual(100, ScenarioSettings.Normal.RecordsPerSecond);
    }

    [TestMethod]
    public void TheStressPresetCarriesFiveHundredVehiclesAtTenReportsAndTenFramesPerSecond()
    {
        Assert.AreEqual(500, ScenarioSettings.Stress.VehicleCount);
        Assert.AreEqual(10, ScenarioSettings.Stress.ReportsPerSecond);
        Assert.AreEqual(10, ScenarioSettings.Stress.FramesPerSecond);
        Assert.AreEqual(5000, ScenarioSettings.Stress.RecordsPerSecond);
    }

    [TestMethod]
    public void TheCeilingPresetCarriesTwoThousandVehiclesAtTenReportsAndTenFramesPerSecond()
    {
        Assert.AreEqual(2000, ScenarioSettings.Ceiling.VehicleCount);
        Assert.AreEqual(10, ScenarioSettings.Ceiling.ReportsPerSecond);
        Assert.AreEqual(10, ScenarioSettings.Ceiling.FramesPerSecond);
        Assert.AreEqual(20000, ScenarioSettings.Ceiling.RecordsPerSecond);
    }

    [TestMethod]
    public void RecordsPerSecondCountsReportsIngestedNotFramesPublished()
    {
        var settings = new ScenarioSettings("test", 100, 5, 1);

        Assert.AreEqual(500, settings.RecordsPerSecond);
    }

    [TestMethod]
    public void RostersBeyondTwoThousandVehiclesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ScenarioSettings("test", ScenarioSettings.MaxVehicleCount + 1, 2, 2));
    }

    [TestMethod]
    public void ReportRatesBeyondTwentyPerSecondAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ScenarioSettings("test", 50, ScenarioSettings.MaxReportsPerSecond + 1, 2));
    }

    [TestMethod]
    public void FrameRatesBeyondTwentyPerSecondAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ScenarioSettings("test", 50, 2, ScenarioSettings.MaxFramesPerSecond + 1));
    }

    [TestMethod]
    public void BaselineRunsTheStressPresetWhileEveryOtherModeRunsNormal()
    {
        Assert.AreSame(ScenarioSettings.Stress, ScenarioSettings.ForMode(DemoMode.Baseline));
        foreach (DemoMode mode in Enum.GetValues<DemoMode>().Where(mode => mode != DemoMode.Baseline))
        {
            Assert.AreSame(ScenarioSettings.Normal, ScenarioSettings.ForMode(mode));
        }
    }

    [TestMethod]
    public void TheQuietPresetCarriesFiveHundredVehiclesAtOneReportAndOneFramePerSecond()
    {
        // E3 control: the stress roster at a tenth of the stress rates, so receive work drops
        // while a map drain still carries up to 500 dirty features.
        Assert.AreEqual(500, ScenarioSettings.Quiet.VehicleCount);
        Assert.AreEqual(1, ScenarioSettings.Quiet.ReportsPerSecond);
        Assert.AreEqual(1, ScenarioSettings.Quiet.FramesPerSecond);
        Assert.AreEqual(500, ScenarioSettings.Quiet.RecordsPerSecond);
    }

    [TestMethod]
    public void TryByNameResolvesKnownNamesAndRejectsUnknownOnes()
    {
        Assert.IsTrue(ScenarioSettings.TryByName("Stress", out ScenarioSettings stress));
        Assert.AreSame(ScenarioSettings.Stress, stress);
        Assert.IsTrue(ScenarioSettings.TryByName("quiet", out ScenarioSettings quiet));
        Assert.AreSame(ScenarioSettings.Quiet, quiet);
        Assert.IsFalse(ScenarioSettings.TryByName("turbo", out ScenarioSettings fallback));
        Assert.AreSame(ScenarioSettings.Normal, fallback);
        Assert.IsFalse(ScenarioSettings.TryByName(null, out _));
    }

    [TestMethod]
    public void PresetNamesResolveCaseInsensitively()
    {
        Assert.AreSame(ScenarioSettings.Stress, ScenarioSettings.ByName("STRESS"));
    }

    [TestMethod]
    public void UnknownPresetNamesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ScenarioSettings.ByName("turbo"));
    }

    [TestMethod]
    public void EveryModeHasPresenterTextWithinTheLengthCaps()
    {
        foreach (DemoMode mode in Enum.GetValues<DemoMode>())
        {
            string name = mode.DisplayName();
            string watching = mode.WatchingLine();
            string cue = mode.DemonstrationCue();

            Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"{mode} needs a display name.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(watching), $"{mode} needs a watching line.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(cue), $"{mode} needs a demonstration cue.");
            Assert.IsTrue(name.Length <= MaxDisplayNameLength,
                $"{mode}'s display name exceeds {MaxDisplayNameLength} characters.");
            Assert.IsTrue(watching.Length <= MaxWatchingLineLength,
                $"{mode}'s watching line exceeds {MaxWatchingLineLength} characters.");
            Assert.IsTrue(cue.Length <= MaxDemonstrationCueLength,
                $"{mode}'s cue exceeds {MaxDemonstrationCueLength} characters.");
        }
    }

    [TestMethod]
    public void AllEightDisplayNamesAreDistinct()
    {
        string[] names = Enum.GetValues<DemoMode>().Select(mode => mode.DisplayName()).ToArray();

        Assert.HasCount(8, names);
        Assert.AreEqual(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    [DataRow(DemoMode.Ready, "ready", "Finished dashboard")]
    [DataRow(DemoMode.Baseline, "baseline", "Raw queue")]
    [DataRow(DemoMode.Pipeline, "pipeline", "Raw queue with 250 record cap per tick")]
    [DataRow(DemoMode.Latest, "latest", "Keep the latest per truck")]
    [DataRow(DemoMode.Batched, "batched", "Batched ApplyEdits calls")]
    [DataRow(DemoMode.Scheduled, "scheduled", "Batched ApplyEdits with 250 ms tick")]
    [DataRow(DemoMode.Viewport, "viewport", "Filter by view extent")]
    [DataRow(DemoMode.Resilient, "resilient", "Failure scenarios")]
    public void PresenterNamesPreserveTheRecordedUrlTokens(DemoMode mode, string token, string name)
    {
        Assert.AreEqual(name, mode.DisplayName());
        Assert.AreEqual(token, mode.Label());
        Assert.IsTrue(DemoModeParser.TryParse(mode.Label(), out DemoMode parsed));
        Assert.AreEqual(mode, parsed);
    }

    [TestMethod]
    public void ModeNamesParseCaseInsensitivelyAndRoundTrip()
    {
        foreach (DemoMode expected in Enum.GetValues<DemoMode>())
        {
            Assert.IsTrue(DemoModeParser.TryParse(expected.Label(), out DemoMode parsed));
            Assert.AreEqual(expected, parsed);
            Assert.IsTrue(DemoModeParser.TryParse(expected.Label().ToUpperInvariant(), out DemoMode upper));
            Assert.AreEqual(expected, upper);
        }
    }

    [TestMethod]
    public void UnrecognizedModeNamesFailToParse()
    {
        Assert.IsFalse(DemoModeParser.TryParse("turbo", out _));
        Assert.IsFalse(DemoModeParser.TryParse("7", out _));
        Assert.IsFalse(DemoModeParser.TryParse(null, out _));
        Assert.IsFalse(DemoModeParser.TryParse("", out _));
    }
}
