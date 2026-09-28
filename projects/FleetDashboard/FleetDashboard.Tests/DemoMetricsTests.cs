using FleetDashboard.Client.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class DemoMetricsTests
{
    [TestMethod]
    public void IncomingRateCountsRecordsWithinTheFixedWindow()
    {
        var clock = new FakeTimeProvider();
        var metrics = new DemoMetrics(clock);

        metrics.RecordIncoming(100);
        clock.AdvanceSeconds(2);
        metrics.RecordIncoming(50);

        Assert.AreEqual(30, metrics.IncomingRecordsPerSecond, 0.01);
    }

    [TestMethod]
    public void RatesExcludeBucketsOlderThanTheWindow()
    {
        var clock = new FakeTimeProvider();
        var metrics = new DemoMetrics(clock);

        metrics.RecordIncoming(500);
        clock.AdvanceSeconds(30);

        Assert.AreEqual(0, metrics.IncomingRecordsPerSecond, 0.01);
    }

    [TestMethod]
    public void MapCallsAndFailuresAccumulate()
    {
        var metrics = new DemoMetrics();

        metrics.RecordMapCall(12, 4.5);
        metrics.RecordMapCall(3, 2.5);
        metrics.RecordFailure();

        Assert.AreEqual(2, metrics.MapCallsPerSecond * 5, 0.01);
        Assert.AreEqual(15, metrics.FeaturesSubmitted);
        Assert.AreEqual(1, metrics.Failures);
    }

    [TestMethod]
    public void ApplyDurationReportsMedianAndP95FromTheRecentSamples()
    {
        var metrics = new DemoMetrics();
        for (int millisecond = 1; millisecond <= 100; millisecond++)
        {
            metrics.RecordMapCall(1, millisecond);
        }

        (double median, double p95) = metrics.ApplyDurationMilliseconds;
        Assert.AreEqual(50.5, median, 0.01);
        Assert.AreEqual(95, p95, 0.01);
    }

    [TestMethod]
    public void ApplyDurationSamplesAreBoundedToTheMostRecent()
    {
        var metrics = new DemoMetrics();
        for (int millisecond = 1; millisecond <= 150; millisecond++)
        {
            metrics.RecordMapCall(1, millisecond);
        }

        (double median, _) = metrics.ApplyDurationMilliseconds;
        Assert.AreEqual(100.5, median, 0.01);
    }

    [TestMethod]
    public void NonFiniteOrNegativeApplyDurationsAreIgnored()
    {
        var metrics = new DemoMetrics();

        metrics.RecordMapCall(1, double.NaN);
        metrics.RecordMapCall(1, -3);
        metrics.RecordMapCall(1, 5);

        (double median, _) = metrics.ApplyDurationMilliseconds;
        Assert.AreEqual(5, median, 0.01);
    }

    [TestMethod]
    public void ZeroCountIncomingRecordsAreIgnored()
    {
        var clock = new FakeTimeProvider();
        var metrics = new DemoMetrics();

        metrics.RecordIncoming(0);
        metrics.RecordIncoming(25);

        Assert.AreEqual(5, metrics.IncomingRecordsPerSecond, 0.01);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void AdvanceSeconds(double seconds) => _utcNow = _utcNow.AddSeconds(seconds);
    }
}
