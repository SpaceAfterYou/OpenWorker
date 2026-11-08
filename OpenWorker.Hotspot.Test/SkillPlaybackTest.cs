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
}

internal static class SkillAnimationsExtensions
{
    public static SkillAnimations WithStart(this SkillAnimations animations, ActionAnimation start)
    {
        return animations with { Start = start };
    }
}
