using FleetDashboard.Client.Rendering;
using FleetDashboard.Client.Streaming;
using FleetDashboard.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FleetDashboard.Tests;


[TestClass]
public sealed class DemoModesTests
{
    [TestMethod]
    public void ThePipelineModesDrainTheMapMoreOftenThanTheOthers()
    {
        Assert.AreEqual(TimeSpan.FromMilliseconds(100), DemoMode.Latest.MapCadence());
        Assert.AreEqual(TimeSpan.FromMilliseconds(100), DemoMode.Batched.MapCadence());
        Assert.AreEqual(TimeSpan.FromMilliseconds(100), DemoMode.Pipeline.MapCadence());
        Assert.AreEqual(TimeSpan.FromMilliseconds(100), DemoMode.Baseline.MapCadence());
        Assert.AreEqual(TimeSpan.FromMilliseconds(250), DemoMode.Ready.MapCadence());
        Assert.AreEqual(TimeSpan.FromMilliseconds(250), DemoMode.Scheduled.MapCadence());
        Assert.AreEqual(TimeSpan.FromMilliseconds(250), DemoMode.Resilient.MapCadence());
    }

    [TestMethod]
    public void OnlyTheRawQueueModesKeepEveryRecord()
    {
        Assert.IsTrue(DemoMode.Pipeline.UsesRawQueue());
        Assert.IsTrue(DemoMode.Baseline.UsesRawQueue());
        Assert.IsFalse(DemoMode.Ready.UsesRawQueue());
        Assert.IsFalse(DemoMode.Latest.UsesRawQueue());
        Assert.IsFalse(DemoMode.Batched.UsesRawQueue());
        Assert.IsFalse(DemoMode.Scheduled.UsesRawQueue());
        Assert.IsFalse(DemoMode.Resilient.UsesRawQueue());
    }

    [TestMethod]
    public void TheRawPipelineModesSubmitOneEditPerVehicle()
    {
        Assert.AreEqual(MapEditGranularity.Individual, DemoMode.Pipeline.EditGranularity());
        Assert.AreEqual(MapEditGranularity.Individual, DemoMode.Baseline.EditGranularity());
        Assert.AreEqual(MapEditGranularity.Individual, DemoMode.Latest.EditGranularity());
        Assert.AreEqual(MapEditGranularity.Collection, DemoMode.Ready.EditGranularity());
        Assert.AreEqual(MapEditGranularity.Collection, DemoMode.Batched.EditGranularity());
        Assert.AreEqual(MapEditGranularity.Collection, DemoMode.Scheduled.EditGranularity());
        Assert.AreEqual(MapEditGranularity.Collection, DemoMode.Resilient.EditGranularity());
    }
}
