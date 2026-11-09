using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ChannelUtilsTest
{
    /// <summary>
    /// A channel is as busy as the level whose threshold its players pass: at the threshold it stays a level lower; past
    /// it, by one player or by half of one in the average of several channels, it goes up.
    /// </summary>
    [TestMethod]
    public void ChannelIsAsBusyAsItsPlayersSayAtEachThreshold()
    {
        (int Threshold, ChannelWorkload Below, ChannelWorkload Above)[] thresholds =
        [
            (ChannelModuleDefinitions.Workload.Normal, ChannelWorkload.Low, ChannelWorkload.Normal),
            (ChannelModuleDefinitions.Workload.High, ChannelWorkload.Normal, ChannelWorkload.High),
            (ChannelModuleDefinitions.Workload.Full, ChannelWorkload.High, ChannelWorkload.Full)
        ];

        Assert.AreEqual(ChannelWorkload.Low, ChannelUtils.GetWorkload(0));

        foreach (var (threshold, below, above) in thresholds)
        {
            Assert.AreEqual(below, ChannelUtils.GetWorkload(threshold), $"{threshold} players");
            Assert.AreEqual(above, ChannelUtils.GetWorkload(threshold + 0.5), $"{threshold} and a half players on average");
            Assert.AreEqual(above, ChannelUtils.GetWorkload(threshold + 1), $"{threshold} and one players");
        }

        Assert.AreEqual(ChannelWorkload.Full, ChannelUtils.GetWorkload(500));
    }
}
