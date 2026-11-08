using System.Numerics;
using OpenWorker.Lua.Ai;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SectorNavigationTest
{
    private static SectorNavigation Rooms()
    {
        return new SectorNavigation(
        [
            (new Vector2(0, 0), new Vector2(1000, 1000)),
            (new Vector2(2000, 1000), new Vector2(1000, 0))
        ]);
    }

    [TestMethod]
    public void ACreatureStaysInItsRoom()
    {
        var target = Rooms().Clamp(new Vector3(500, 500, 10), new Vector3(1500, 500, 10));

        Assert.IsNotNull(target);
        Assert.IsLessThan(1000f, target.Value.X);
        Assert.AreEqual(500f, target.Value.Y);
        Assert.AreEqual(10f, target.Value.Z);
    }

    [TestMethod]
    public void ACreatureOutsideEveryRoomIsFree()
    {
        var target = Rooms().Clamp(new Vector3(-500, -500, 0), new Vector3(5000, 5000, 0));

        Assert.AreEqual(new Vector3(5000, 5000, 0), target);
    }

    [TestMethod]
    public void NothingIsSeenThroughTheWallOfARoom()
    {
        var rooms = Rooms();

        Assert.IsTrue(rooms.CanSee(new Vector3(100, 100, 0), new Vector3(900, 900, 0)));
        Assert.IsFalse(rooms.CanSee(new Vector3(500, 500, 0), new Vector3(1500, 500, 0)));
        Assert.IsTrue(rooms.CanSee(new Vector3(500, 500, 0), new Vector3(-500, 500, 0)), "Somebody who stands where there is no room is not hidden.");
    }

    [TestMethod]
    public void ABoxTooNarrowForTheMarginKeepsTheCreatureInItsMiddle()
    {
        var narrow = new SectorNavigation([(new Vector2(0, 0), new Vector2(50, 50))]);

        Assert.AreEqual(new Vector3(25, 25, 0), narrow.Clamp(new Vector3(10, 10, 0), new Vector3(500, 500, 0)));
    }
}
