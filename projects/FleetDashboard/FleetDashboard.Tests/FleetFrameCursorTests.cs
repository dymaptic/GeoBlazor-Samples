using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class FleetFrameCursorTests
{
    [TestMethod]
    public void ARepeatedFrameIsNotAdvancedTwice()
    {
        var cursor = new FleetFrameCursor();

        Assert.IsTrue(cursor.TryAdvance(Frame(RunId, 4)));
        Assert.IsFalse(cursor.TryAdvance(Frame(RunId, 4)));
    }

    [TestMethod]
    public void AnOutOfOrderFrameIsIgnoredWhileTheNextOneAdvances()
    {
        var cursor = new FleetFrameCursor();
        _ = cursor.TryAdvance(Frame(RunId, 4));

        Assert.IsFalse(cursor.TryAdvance(Frame(RunId, 3)));
        Assert.IsTrue(cursor.TryAdvance(Frame(RunId, 5)));
    }

    [TestMethod]
    public void ANewRunStartsAGenuineFrameStream()
    {
        var cursor = new FleetFrameCursor();
        _ = cursor.TryAdvance(Frame(RunId, 40));

        Assert.IsTrue(cursor.TryAdvance(Frame(Guid.NewGuid(), 1)));
    }

    private static FleetFrame Frame(Guid runId, long frameSequence)
    {
        DateTimeOffset publishedAtUtc = DateTimeOffset.Parse("2026-09-08T12:00:00Z");
        return new FleetFrame(runId, frameSequence, publishedAtUtc,
            [new VehicleReport(7, frameSequence, publishedAtUtc, -75.34, 41.09, 42, "swiftwater")]);
    }

    private static readonly Guid RunId = Guid.Parse("10000000-0000-0000-0000-000000000001");
}
