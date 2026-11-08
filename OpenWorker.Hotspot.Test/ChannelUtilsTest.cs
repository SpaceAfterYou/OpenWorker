using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ChannelUtilsTest
{
    [TestMethod]
    [DataRow(0, ChannelWorkload.Low)]
    [DataRow(ChannelModuleDefinitions.Workload.Normal, ChannelWorkload.Low)]
    [DataRow(ChannelModuleDefinitions.Workload.Normal + 1, ChannelWorkload.Normal)]
    [DataRow(ChannelModuleDefinitions.Workload.High, ChannelWorkload.Normal)]
    [DataRow(ChannelModuleDefinitions.Workload.High + 1, ChannelWorkload.High)]
    [DataRow(ChannelModuleDefinitions.Workload.Full, ChannelWorkload.High)]
    [DataRow(ChannelModuleDefinitions.Workload.Full + 1, ChannelWorkload.Full)]
    public void ChannelIsAsBusyAsItsPlayersSayAtEachThreshold(int count, ChannelWorkload expected)
    {
        Assert.AreEqual(expected, ChannelUtils.GetWorkload(count));
    }

    [TestMethod]
    public void ChannelWithMoreThanFullPlayersStaysFull()
    {
        Assert.AreEqual(ChannelWorkload.Full, ChannelUtils.GetWorkload(500));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(ChannelModuleDefinitions.Workload.Normal)]
    [DataRow(ChannelModuleDefinitions.Workload.Normal + 1)]
    [DataRow(ChannelModuleDefinitions.Workload.High)]
    [DataRow(ChannelModuleDefinitions.Workload.High + 1)]
    [DataRow(ChannelModuleDefinitions.Workload.Full)]
    [DataRow(ChannelModuleDefinitions.Workload.Full + 1)]
    public void IntegerShortAndDoubleCountsGiveTheSameWorkload(int count)
    {
        var fromInt = ChannelUtils.GetWorkload(count);
        var fromShort = ChannelUtils.GetWorkload((short)count);
        var fromDouble = ChannelUtils.GetWorkload((double)count);

        Assert.AreEqual(fromInt, fromShort);
        Assert.AreEqual(fromInt, fromDouble);
    }

    [TestMethod]
    [DataRow(ChannelModuleDefinitions.Workload.Normal + 0.5, ChannelWorkload.Normal)]
    [DataRow(ChannelModuleDefinitions.Workload.High + 0.5, ChannelWorkload.High)]
    [DataRow(ChannelModuleDefinitions.Workload.Full + 0.5, ChannelWorkload.Full)]
    [DataRow(ChannelModuleDefinitions.Workload.Normal - 0.5, ChannelWorkload.Low)]
    [DataRow(ChannelModuleDefinitions.Workload.Full - 0.5, ChannelWorkload.High)]
    public void AverageOfSeveralChannelsIsClassifiedByTheSameThresholds(double average, ChannelWorkload expected)
    {
        Assert.AreEqual(expected, ChannelUtils.GetWorkload(average));
    }
}
