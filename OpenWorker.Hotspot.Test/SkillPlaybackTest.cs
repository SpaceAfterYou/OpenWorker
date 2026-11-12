using System.Numerics;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SkillPlaybackTest
{
    private static AttackTrigger Attack(int id, float start)
    {
        return new AttackTrigger(id, 0, 0, 0, start, 360f, Vector3.Zero, 0, 0, 100, 100, 0);
    }

    private static AttackTrigger Shot(int id, float start)
    {
        return Attack(id, start) with { AttackType = 1 };
    }

    private static ActionAnimation Clip(string name, float length, bool repeats, params AttackTrigger[] attacks)
    {
        return new ActionAnimation(name, length, attacks, repeats);
    }

    private static SkillAnimations Skill(
        ActionAnimation? start,
        ActionAnimation? loop,
        ActionAnimation? extra,
        ActionAnimation? end,
        float loopTime = 0f,
        bool pressed = false,
        float[]? charge = null,
        ActionAnimation?[]? ends = null)
    {
        return new SkillAnimations(start, loop, extra, end, loopTime, pressed, charge ?? [], ends ?? []);
    }

    /// <summary>
    /// The playback names the animation that plays now and the moment it began, so that what the animation says about armor can
    /// be read at any time; once the skill is over it names none.
    /// </summary>
    [TestMethod]
    public void ThePlaybackNamesTheAnimationThatPlaysNow()
    {
        var start = Clip("Start", 1f, false);
        var end = Clip("End", 2f, false);
        var playback = new SkillPlayback(Skill(start, null, null, end));

        Assert.AreEqual(("Start", 0f), (playback.Playing!.Value.Clip.Name, playback.Playing.Value.Began));

        playback.Advance(1.5f);

        Assert.AreEqual(("End", 1f), (playback.Playing!.Value.Clip.Name, playback.Playing.Value.Began));

        playback.Advance(3.5f);

        Assert.IsNull(playback.Playing);
    }

    /// <summary>
    /// Runs the skill to the end, letting go and holding the key at the given moments. Returns the moments of the attacks.
    /// </summary>
    private static List<(float Time, int Id)> Play(SkillPlayback playback, params (float At, bool Pressed)[] keys)
    {
        var hits = new List<(float, int)>();
        var pending = new Queue<(float At, bool Pressed)>(keys.OrderBy(e => e.At));

        for (var guard = 0; guard < 200 && !playback.IsDone; guard++)
        {
            var next = playback.NextTime ?? 0f;

            if (pending.TryPeek(out var key) && key.At <= next)
            {
                pending.Dequeue();

                hits.AddRange(playback.Advance(key.At).Select(e => (e.Time, e.Trigger.Id)));
                playback.Press(key.Pressed, key.At);
                continue;
            }

            hits.AddRange(playback.Advance(next).Select(e => (e.Time, e.Trigger.Id)));
        }

        return hits;
    }

    private static void AreTimes(float[] expected, List<(float Time, int Id)> actual)
    {
        Assert.HasCount(expected.Length, actual, string.Join(", ", actual.Select(e => $"{e.Time:0.00}/{e.Id}")));

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], actual[i].Time, 0.001f, $"attack {i}");
        }
    }

    [TestMethod]
    public void StartAndEndPlayOneAfterTheOther()
    {
        var playback = new SkillPlayback(Skill(Clip("start", 0.5f, false, Attack(1, 0.2f)), null, null, Clip("end", 0.3f, false, Attack(2, 0.1f))));

        var hits = Play(playback);

        AreTimes([0.2f, 0.6f], hits);
        Assert.IsTrue(playback.IsDone);
        Assert.IsNull(playback.NextTime);
    }

    [TestMethod]
    public void SkillWithoutAStartBeginsWithItsEnd()
    {
        var playback = new SkillPlayback(Skill(null, null, null, Clip("end", 0.3f, false, Attack(2, 0.1f))));

        AreTimes([0.1f], Play(playback));
    }

    [TestMethod]
    public void LoopStartsOverForAsLongAsTheTableSays()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, true, Attack(1, 0.1f)),
            null,
            Clip("end", 0.3f, false, Attack(2, 0.1f)),
            loopTime: 1f));

        // Three rounds of the loop begin within a second, and the attack of each comes before the loop is over.
        AreTimes([0.6f, 1.0f, 1.4f, 1.6f], Play(playback));
    }

    [TestMethod]
    public void LoopThatDoesNotRepeatPlaysOnce()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, false, Attack(1, 0.1f)),
            null,
            Clip("end", 0.3f, false),
            loopTime: 5f));

        AreTimes([0.6f], Play(playback));
    }

    [TestMethod]
    public void LoopThatIsLongerThanItsTimeIsCut()
    {
        var playback = new SkillPlayback(Skill(
            null,
            Clip("loop", 6.9f, true, Attack(1, 0.0f), Attack(2, 5.0f)),
            null,
            Clip("end", 0.3f, false),
            loopTime: 3f).WithStart(Clip("start", 0.1f, false)));

        AreTimes([0.1f], Play(playback));
    }

    [TestMethod]
    public void LettingGoOfTheKeyEndsTheLoopAtOnce()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, true, Attack(1, 0.1f)),
            null,
            Clip("end", 0.3f, false, Attack(2, 0.1f)),
            pressed: true));

        // Held without a time of its own: the loop goes on until the key is let go at 1.2.
        AreTimes([0.6f, 1.0f, 1.3f], Play(playback, (1.2f, false)));
    }

    [TestMethod]
    public void LoopWithoutATimeAndWithoutTheKeyPlaysOnce()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, true, Attack(1, 0.1f)),
            null,
            Clip("end", 0.3f, false)));

        AreTimes([0.6f], Play(playback));
    }

    [TestMethod]
    public void WaitEndsTheSkillWhenTheKeyIsNeverHeld()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, true, Attack(1, 0.1f)),
            Clip("wait", 1.2f, true),
            Clip("end", 0.3f, false, Attack(2, 0.1f)),
            loopTime: 1f));

        // The wait lasts three seconds, then the end.
        AreTimes([3.6f], Play(playback));
    }

    [TestMethod]
    public void HoldingTheKeyDuringTheWaitStartsTheLoop()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, true, Attack(1, 0.1f)),
            Clip("wait", 1.2f, true),
            Clip("end", 0.3f, false, Attack(2, 0.1f)),
            loopTime: 0.8f));

        // Held at 1.0: the loop plays two rounds of 0.4, then the end.
        AreTimes([1.1f, 1.5f, 1.9f], Play(playback, (1.0f, true)));
    }

    [TestMethod]
    public void KeyHeldDuringTheStartCarriesIntoTheLoop()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("loop", 0.4f, true, Attack(1, 0.1f)),
            Clip("wait", 1.2f, true),
            Clip("end", 0.3f, false),
            loopTime: 0.4f));

        AreTimes([0.6f], Play(playback, (0.2f, true)));
    }

    [TestMethod]
    public void NextTimeIsTheNearestMoment()
    {
        var playback = new SkillPlayback(Skill(Clip("start", 0.5f, false, Attack(1, 0.2f)), null, null, Clip("end", 0.3f, false)));

        Assert.AreEqual(0.2f, playback.NextTime);

        playback.Advance(0.2f);

        Assert.AreEqual(0.5f, playback.NextTime);
    }

    [TestMethod]
    public void ChargedSkillEndsWithTheLevelThePlayerReached()
    {
        ActionAnimation?[] ends = [Clip("end_01", 0.3f, false, Attack(11, 0.1f)), Clip("end_02", 0.3f, false, Attack(12, 0.1f)), Clip("end_03", 0.3f, false, Attack(13, 0.1f))];

        SkillPlayback Charged() => new(Skill(
            Clip("start", 0.5f, false),
            Clip("charge", 0.4f, true),
            null,
            Clip("end", 0.3f, false, Attack(10, 0.1f)),
            pressed: true,
            charge: [1f, 2f, 3f],
            ends: ends));

        // The charge is held from the start of the loop at 0.5.
        Assert.AreEqual(11, Play(Charged(), (1.0f, false)).Single().Id, "half a second: the first level");
        Assert.AreEqual(12, Play(Charged(), (2.0f, false)).Single().Id, "a second and a half: the second level");
        Assert.AreEqual(13, Play(Charged(), (9.0f, false)).Single().Id, "long enough: the last level");
    }

    [TestMethod]
    public void ChargeLevelWithoutAnEndFallsBackToTheCommonOne()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false),
            Clip("charge", 0.4f, true),
            null,
            Clip("end", 0.3f, false, Attack(10, 0.1f)),
            pressed: true,
            charge: [1f, 2f],
            ends: [null, null]));

        Assert.AreEqual(10, Play(playback, (1.0f, false)).Single().Id);
    }

    /// <summary>
    /// The client fires the shots of a skill itself, and the skill takes no more of them than its animations let go: each play of
    /// an animation, and each round of one that repeats, once. A shot of the round or the animation about to begin is taken a
    /// moment early, and one of a skill that is over only a moment late.
    /// </summary>
    [TestMethod]
    public void ASkillTakesTheShotsItsAnimationsLetGo()
    {
        var playback = new SkillPlayback(Skill(
            Clip("start", 0.5f, false, Shot(1, 0.2f)),
            Clip("loop", 0.4f, true, Shot(2, 0f)),
            null,
            Clip("end", 0.3f, false),
            loopTime: 0.8f));

        playback.Advance(0.1f);

        Assert.IsTrue(playback.TakeShot(1, 0.1f), "The start lets its shot go as it begins.");
        Assert.IsFalse(playback.TakeShot(1, 0.2f), "It lets it go once.");
        Assert.IsTrue(playback.TakeShot(2, 0.3f), "The loop begins at 0.5: its shot comes a moment early.");
        Assert.IsFalse(playback.TakeShot(2, 0.3f), "The first round has none left.");

        playback.Advance(0.55f);

        Assert.IsFalse(playback.TakeShot(2, 0.55f), "The shot of the first round was fired before it began, and the second is not near.");

        playback.Advance(0.9f);

        Assert.IsTrue(playback.TakeShot(2, 0.9f), "The second round lets one more go.");
        Assert.IsFalse(playback.TakeShot(3, 0.9f), "The skill has no such shot.");

        playback.Advance(1.8f);

        Assert.IsTrue(playback.IsDone);
        Assert.IsFalse(playback.TakeShot(2, 1.8f), "Two rounds, two shots.");

        var late = new SkillPlayback(Skill(null, null, null, Clip("end", 0.3f, false, Shot(4, 0.2f))));

        late.Advance(0.5f);

        Assert.IsTrue(late.TakeShot(4, 0.5f), "A moment after the end the shot is still the skill's.");

        var over = new SkillPlayback(Skill(null, null, null, Clip("end", 0.3f, false, Shot(4, 0.2f))));

        over.Advance(1f);

        Assert.IsFalse(over.TakeShot(4, 1f), "Long after the end it is not.");
    }

    /// <summary>
    /// A shot comes at its own moment of the animation, not as the animation begins: a client that fires it sooner than a moment
    /// before is refused, so starting a skill again and again does not fire all of its shots at once.
    /// </summary>
    [TestMethod]
    public void AShotComesAtItsOwnMoment()
    {
        var playback = new SkillPlayback(Skill(null, null, null, Clip("end", 2f, false, Shot(1, 1f))));

        playback.Advance(0.5f);

        Assert.IsFalse(playback.TakeShot(1, 0.5f), "Half a second before its moment.");

        playback.Advance(0.75f);

        Assert.IsTrue(playback.TakeShot(1, 0.75f), "A moment before it.");
    }

    /// <summary>
    /// Another skill cuts the one that plays only from the moment its animation may be cut, as the client lets the player (a
    /// moment early for a client ahead, a moment after the animation gave way for one behind), or as the skill ends.
    /// </summary>
    [TestMethod]
    public void AnotherSkillCutsTheSkillOnlyOnceItsMomentHasCome()
    {
        var start = Clip("start", 1f, false) with { Cuts = [new CutEvent(0.6f, 0.9f, 0.7f)] };
        var playback = new SkillPlayback(Skill(start, null, null, Clip("end", 1f, false)));

        playback.Advance(0.3f);

        Assert.IsFalse(playback.MayCut(0.3f), "The start may be cut from 0.7.");

        playback.Advance(0.45f);

        Assert.IsTrue(playback.MayCut(0.45f), "A moment before.");

        playback.Advance(1.2f);

        Assert.IsTrue(playback.MayCut(1.2f), "A moment after the start gave way to the end.");

        playback.Advance(1.5f);

        Assert.IsFalse(playback.MayCut(1.5f), "The end may not be cut.");

        playback.Advance(1.75f);

        Assert.IsTrue(playback.MayCut(1.75f), "The skill is about to end.");
    }

    /// <summary>
    /// The animations play at the attack speed of the player, and a stretch with a speed of its own at that speed.
    /// </summary>
    [TestMethod]
    public void AnimationsPlayAtTheAttackSpeedAndAtTheSpeedOfTheirStretches()
    {
        var clip = Clip("end", 2f, false, Attack(1, 0.4f), Attack(2, 1.2f)) with { Speeds = [new SpeedEvent(0f, 0.4f, 4f, false)] };

        // Twice as fast: the stretch to 0.4 at four times its speed takes 0.1, the 0.8 after it 0.4.
        AreTimes([0.1f, 0.5f], Play(new SkillPlayback(Skill(null, null, null, clip), 2f)));
    }

    /// <summary>
    /// A blow that lands holds the animation of the attacker for its time: what was to come comes that much later.
    /// </summary>
    [TestMethod]
    public void ABlowThatLandsHoldsTheAnimation()
    {
        var playback = new SkillPlayback(Skill(null, null, null, Clip("end", 1f, false, Attack(1, 0.2f), Attack(2, 0.6f))));

        playback.Advance(0.2f);
        playback.Pause(0.1f);

        Assert.AreEqual(0.7f, playback.NextTime!.Value, 0.001f);
    }
}

internal static class SkillAnimationsExtensions
{
    public static SkillAnimations WithStart(this SkillAnimations animations, ActionAnimation start)
    {
        return animations with { Start = start };
    }
}
