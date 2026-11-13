using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The countdown window of a maze is counted on the game time, and a player who comes in while it runs is shown what is left.
/// </summary>
[TestClass]
public sealed class CasualRaidCountdownTest
{
    private static int Run(CasualRaidCountdown countdown, float seconds, float step = 0.5f)
    {
        var shown = 0;

        for (var time = 0f; time < seconds - step / 2f; time += step)
        {
            if (countdown.Step(step))
            {
                shown++;
            }
        }

        return shown;
    }

    [TestMethod]
    public void APlayerWhoComesInLaterIsShownTheTimeLeftAfterFiveSeconds()
    {
        var countdown = new CasualRaidCountdown();

        countdown.Shown(0, 60f);
        Run(countdown, 10f);

        countdown.Arrived();

        Assert.AreEqual(0, Run(countdown, 4.5f), "the client gets its window ready first");
        Assert.IsTrue(countdown.Step(0.5f));
        Assert.AreEqual(0, countdown.Value);
        Assert.AreEqual(45f, countdown.Left, 0.01f);
        Assert.AreEqual(0, Run(countdown, 10f), "once for each player who comes in");
    }

    [TestMethod]
    public void ACountdownThatIsHiddenOrOverIsNotShownAgain()
    {
        var countdown = new CasualRaidCountdown();

        countdown.Shown(0, 3f);
        countdown.Arrived();

        Assert.AreEqual(0, Run(countdown, 10f), "the time ran out before the five seconds");

        countdown.Shown(0, 60f);
        countdown.Shown(0, -1f);
        countdown.Arrived();

        Assert.AreEqual(0, Run(countdown, 10f));
    }
}
