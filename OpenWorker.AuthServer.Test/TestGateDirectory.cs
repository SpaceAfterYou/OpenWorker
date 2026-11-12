using OpenWorker.AuthServer.App;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Login.Types;

namespace OpenWorker.AuthServer.Test;

/// <summary>
/// The gate list is renewed on a pool thread while the world loop goes through it for a login: a round of the sync puts a
/// new list in place and never changes the one a login already holds.
/// </summary>
[TestClass]
public sealed class TestGateDirectory
{
    [TestMethod]
    public void ARoundOfTheSyncLeavesTheListALoginHoldsAsItWas()
    {
        var directory = new GateDirectory();

        directory.Publish([Gate(1, GateWorkload.Normal)]);

        var held = directory.Gates;

        directory.Publish([Gate(1, GateWorkload.Offline), Gate(2, GateWorkload.Normal)]);

        Assert.HasCount(1, held);
        Assert.AreEqual(GateWorkload.Normal, held[0].Workload);
        CollectionAssert.AreEqual(new short[] { 1, 2 }, directory.Gates.Select(e => e.Id).ToArray());
        Assert.AreEqual(GateWorkload.Offline, directory.Gates[0].Workload);
    }

    private static GateInfo Gate(short id, GateWorkload workload) => new()
    {
        Id = id,
        Name = "Gate " + id,
        Workload = workload,
        Address = "127.0.0.1",
        Port = 10010,
        OnlineCount = 0
    };
}
