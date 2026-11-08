using OpenWorker.Channel;
using OpenWorker.Hotspot.Cache.Types;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class WorldManagerTest
{
    private const short Location = 10003;

    private static DistrictCache District() => new()
    {
        Guid = Guid.NewGuid(),
        Gate = 1,
        Location = Location,
        Group = 1,
        PositionX = 0,
        PositionY = 0,
        PositionZ = 0,
        Rotation = 0,
        Jump = 1,
        Portal = 1,
        Host = "127.0.0.1",
        Port = 10011
    };

    private static ChannelCache Channel(DistrictCache owner, short identifier, short online) => new()
    {
        Owner = owner.Guid,
        Gate = 1,
        Location = Location,
        Identifier = identifier,
        OnlineCount = online
    };

    [TestMethod]
    public void NoDistrictGivesNoPlace()
    {
        var orphan = Channel(District(), 1, 0);

        Assert.IsNull(WorldManager.Pick([], []));
        Assert.IsNull(WorldManager.Pick([], [orphan]));
    }

    [TestMethod]
    public void DistrictWithoutChannelsGivesNoPlace()
    {
        // A district registers itself before its channels, and the client must be told that nobody can take it yet.
        Assert.IsNull(WorldManager.Pick([District()], []));
    }

    [TestMethod]
    public void ChannelOfAnotherDistrictIsNotUsed()
    {
        var running = District();
        var gone = District();

        Assert.IsNull(WorldManager.Pick([running], [Channel(gone, 1, 0)]));
    }

    [TestMethod]
    public void EmptiestDistrictAndItsEmptiestChannelAreChosen()
    {
        var busy = District();
        var quiet = District();

        var place = WorldManager.Pick(
            [busy, quiet],
            [Channel(busy, 1, 10), Channel(busy, 2, 20), Channel(quiet, 1, 7), Channel(quiet, 2, 3)]);

        Assert.IsNotNull(place);
        Assert.AreEqual(quiet.Guid, place.District.Guid);
        Assert.AreEqual((short)2, place.Channel.Identifier);
    }

    [TestMethod]
    public void DistrictWithoutChannelsDoesNotHideOneWithChannels()
    {
        var starting = District();
        var running = District();

        var place = WorldManager.Pick([starting, running], [Channel(running, 1, 50)]);

        Assert.IsNotNull(place);
        Assert.AreEqual(running.Guid, place.District.Guid);
    }

    [TestMethod]
    public void EqualDistrictsPreferTheOneWithMoreChannels()
    {
        var narrow = District();
        var wide = District();

        var place = WorldManager.Pick(
            [narrow, wide],
            [Channel(narrow, 1, 4), Channel(wide, 1, 2), Channel(wide, 2, 2)]);

        Assert.IsNotNull(place);
        Assert.AreEqual(wide.Guid, place.District.Guid);
    }
}
