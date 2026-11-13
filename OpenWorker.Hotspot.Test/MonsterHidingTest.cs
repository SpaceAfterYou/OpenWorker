using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Lua.Ai;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A monster that has gone from sight.
/// </summary>
[TestClass]
public sealed class MonsterHidingTest
{
    private static readonly ActorValue Monster = new(0x1234, ActorType.Monster);

    /// <summary>
    /// Gone, a monster is out of reach, stands and does not think, except for what its event leaves it.
    /// </summary>
    [TestMethod]
    public void AHiddenMonsterKeepsOnlyWhatItsEventLeavesIt()
    {
        var hiding = new MonsterHiding();

        Assert.IsTrue(hiding.CanBeHit && hiding.CanMove && hiding.CanThink);

        hiding.Begin(new InvisibleEvent(0f, InvisibleEvent.KeepsMind, InvisibleEvent.ForTime, 2000), 10.0);

        Assert.IsFalse(hiding.CanBeHit);
        Assert.IsFalse(hiding.CanMove);
        Assert.IsTrue(hiding.CanThink);

        hiding.Begin(new InvisibleEvent(0f, MonsterHiding.KeepsCollision | MonsterHiding.KeepsMovement, InvisibleEvent.ForTime, 2000), 10.0);

        Assert.IsTrue(hiding.CanBeHit);
        Assert.IsTrue(hiding.CanMove);
        Assert.IsFalse(hiding.CanThink);
    }

    /// <summary>
    /// One who comes in while a monster is gone for a time is told the time that is left; other kinds are told as they were.
    /// </summary>
    [TestMethod]
    public void OneWhoComesInLaterIsToldTheTimeThatIsLeft()
    {
        var hiding = new MonsterHiding();

        hiding.Begin(new InvisibleEvent(0f, 8, InvisibleEvent.ForTime, 2500), 10.0);

        var notice = hiding.Notice(Monster, 11.0);

        Assert.IsTrue(notice.Invisible);
        Assert.AreEqual((8, InvisibleEvent.ForTime, 1500), (notice.Flags, notice.Type, notice.Value));

        hiding.Begin(new InvisibleEvent(0f, 0, InvisibleEvent.UntilHurt, 300), 10.0);

        Assert.AreEqual(300, hiding.Notice(Monster, 30.0).Value);
    }
}
