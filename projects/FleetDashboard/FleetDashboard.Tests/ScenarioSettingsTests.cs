using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class ScenarioSettingsTests
{
    [TestMethod]
    public void TheNormalPresetCarriesFiftyVehiclesAtTwoFramesPerSecond()
    {
        Assert.AreEqual(50, ScenarioSettings.Normal.VehicleCount);
        Assert.AreEqual(2, ScenarioSettings.Normal.FramesPerSecond);
        Assert.AreEqual(100, ScenarioSettings.Normal.RecordsPerSecond);
    }

    [TestMethod]
    public void TheStressPresetCarriesFiveHundredVehiclesAtTenFramesPerSecond()
    {
        Assert.AreEqual(500, ScenarioSettings.Stress.VehicleCount);
        Assert.AreEqual(10, ScenarioSettings.Stress.FramesPerSecond);
        Assert.AreEqual(5000, ScenarioSettings.Stress.RecordsPerSecond);
    }

    [TestMethod]
    public void TheCeilingPresetCarriesTwoThousandVehiclesAtTenFramesPerSecond()
    {
        Assert.AreEqual(2000, ScenarioSettings.Ceiling.VehicleCount);
        Assert.AreEqual(10, ScenarioSettings.Ceiling.FramesPerSecond);
        Assert.AreEqual(20000, ScenarioSettings.Ceiling.RecordsPerSecond);
    }

    [TestMethod]
    public void RostersBeyondTwoThousandVehiclesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ScenarioSettings("test", ScenarioSettings.MaxVehicleCount + 1, 2));
    }

    [TestMethod]
    public void FrameRatesBeyondTwentyPerSecondAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ScenarioSettings("test", 50, ScenarioSettings.MaxFramesPerSecond + 1));
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
    public void TheQuietPresetCarriesFiveHundredVehiclesAtOneFramePerSecond()
    {
        // E3 control: the stress roster at a tenth of the frame rate, so receive work drops
        // while a map drain still carries up to 500 dirty features.
        Assert.AreEqual(500, ScenarioSettings.Quiet.VehicleCount);
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
