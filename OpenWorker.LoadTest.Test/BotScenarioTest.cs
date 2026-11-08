using OpenWorker.LoadTest.Bots;
using OpenWorker.LoadTest.Cli;
using OpenWorker.LoadTest.Reporting;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// The bots run their whole script against stand-in servers that speak with the types of the real ones: they sign in,
/// create a character, enter a district, move, talk, go to a maze and fight, and the figures come out.
/// </summary>
[TestClass]
public sealed class BotScenarioTest
{
    public TestContext TestContext { get; set; } = null!;

    private static RunOptions Options(FakeWorld world, int bots)
    {
        return new RunOptions
        {
            Bots = bots,
            RampUp = 200,
            Warmup = TimeSpan.FromMilliseconds(300),
            Duration = TimeSpan.FromSeconds(2),
            MoveRate = 10,
            ChatRate = 2,
            MazeShare = 0,
            MazeDelay = TimeSpan.FromMilliseconds(300),
            SkillRate = 5,
            AuthPort = world.AuthPort,
            AccountPrefix = world.Prefix,
            Password = world.Password,
            StageTimeout = TimeSpan.FromSeconds(5)
        };
    }

    private static async Task<(RunStats Stats, Fleet Fleet, RunContext Context)> RunAsync(RunOptions options, CancellationToken cancellation)
    {
        var stats = new RunStats();
        var context = new RunContext { Options = options, Stats = stats };
        var fleet = new Fleet(context);

        await fleet.RunAsync(cancellation);

        return (stats, fleet, context);
    }

    private static LatencySummary Total(RunStats stats, Func<BotMetrics, LatencyHistogram> select)
    {
        var total = new LatencyHistogram();

        foreach (var bot in stats.Bots())
        {
            total.Add(select(bot));
        }

        return LatencySummary.Of(total);
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task BotsSignInEnterTheDistrictAndMeasureTheMovesOfEachOther()
    {
        await using var world = FakeWorld.Start();

        var (stats, _, _) = await RunAsync(Options(world, 12), TestContext.CancellationToken);

        Assert.AreEqual(12, stats.Started);
        Assert.AreEqual(0, stats.Failed, string.Join("; ", stats.Errors.Select(e => $"{e.Key} {e.Value.Example}")));
        Assert.AreEqual(12, stats.PeakActive);
        Assert.AreEqual(12, world.Creations, "Every account had no character, and the bots made one each.");
        Assert.AreEqual(12, stats.Stage(BotStage.Login).Succeeded);
        Assert.AreEqual(12, stats.Stage(BotStage.GateList).Succeeded);
        Assert.AreEqual(12, stats.Stage(BotStage.GateEnter).Succeeded);
        Assert.AreEqual(12, stats.Stage(BotStage.Character).Succeeded);
        Assert.AreEqual(12, stats.Stage(BotStage.DistrictEnter).Succeeded);

        var echo = Total(stats, e => e.MoveEcho);
        var delivery = Total(stats, e => e.MoveDelivery);
        var ping = Total(stats, e => e.Ping);

        // 12 bots at 10 moves a second for 2 seconds: about 240 moves, each heard by all 12.
        Assert.IsGreaterThan(150, echo.Count, "Each bot hears its own moves.");
        Assert.IsGreaterThan(echo.Count * 8, delivery.Count, "And each hears the moves of the 11 others.");
        Assert.IsGreaterThan(150, ping.Count);
        Assert.IsGreaterThan(0d, echo.P50Ms);
        Assert.IsLessThan(1000d, echo.P99Ms, "A fake server on the same machine answers in milliseconds.");
        Assert.IsGreaterThanOrEqualTo(echo.P50Ms, echo.P99Ms);
        Assert.IsGreaterThanOrEqualTo(echo.P99Ms, echo.MaxMs);

        var chat = Total(stats, e => e.ChatEcho);

        Assert.IsGreaterThan(0, chat.Count, "The bots hear their own lines.");
        Assert.IsGreaterThan(0, Total(stats, e => e.ChatDelivery).Count, "And the lines of the others.");

        var unmatched = stats.Bots().Sum(e => e.Unmatched);

        Assert.IsLessThan((echo.Count + delivery.Count) / 20, unmatched, "Nearly every broadcast is matched to the move that caused it.");

        var lag = Total(stats, e => e.SendLag);

        Assert.IsGreaterThan(150, lag.Count);
        Assert.IsLessThan(100d, lag.P50Ms, "The bot sends when it is due.");
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task BotsThatFightLeaveTheDistrictForTheMazeAndUseSkills()
    {
        await using var world = FakeWorld.Start();

        var options = Options(world, 10);

        options.MazeShare = 0.2;
        options.Duration = TimeSpan.FromSeconds(3);
        options.Warmup = TimeSpan.FromSeconds(1.5);

        var (stats, _, _) = await RunAsync(options, TestContext.CancellationToken);

        Assert.AreEqual(0, stats.Failed, string.Join("; ", stats.Errors.Select(e => $"{e.Key} {e.Value.Example}")));
        Assert.AreEqual(2, stats.Stage(BotStage.MazeEnter).Succeeded, "10 bots, a fifth of them fight.");
        Assert.AreEqual(0, stats.Stage(BotStage.MazeEnter).Failed);
        Assert.AreEqual(2, world.MazeCompletes);
        Assert.IsGreaterThan(0, world.Skills);

        Assert.IsGreaterThan(0, Total(stats, e => e.MazeMoveEcho).Count);
        Assert.AreEqual(0L, stats.Bots().Sum(e => e.Unmatched), "The moves of the monsters of a maze are not moves of a bot left unmatched.");

        var skills = Total(stats, e => e.SkillResponse);

        Assert.IsGreaterThan(0, skills.Count);
        Assert.IsLessThan(1000d, skills.P99Ms);

        // The skill the bots use is the first one their character learned: the district told them.
        Assert.AreEqual(stats.Bots().Sum(e => e.SkillsSent), stats.Bots().Sum(e => e.SkillsAnswered), 3d);
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task BotsStayInTheDistrictWhenTheMazeRefusesThem()
    {
        await using var world = FakeWorld.Start();

        world.RefuseMaze = true;

        var options = Options(world, 5);

        options.MazeShare = 1;

        var (stats, _, _) = await RunAsync(options, TestContext.CancellationToken);

        Assert.AreEqual(5, stats.Stage(BotStage.MazeEnter).Failed);
        Assert.AreEqual(0, stats.Lost, "A refused maze is not a lost bot.");
        Assert.AreEqual(5, stats.PeakActive);
        Assert.IsTrue(stats.Errors.ContainsKey("maze.enter: refused-50003"), string.Join(", ", stats.Errors.Keys));
        Assert.IsGreaterThan(0, Total(stats, e => e.MoveEcho).Count, "They go on moving in the district.");
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task BotsTravelToAnotherDistrictWhenAsked()
    {
        await using var world = FakeWorld.Start();

        var options = Options(world, 6);

        options.District = FakeWorld.SecondLocation;
        options.Channel = 3;

        var (stats, _, _) = await RunAsync(options, TestContext.CancellationToken);

        Assert.AreEqual(0, stats.Failed, string.Join("; ", stats.Errors.Select(e => $"{e.Key} {e.Value.Example}")));
        Assert.AreEqual(6, stats.Stage(BotStage.DistrictTravel).Succeeded);
        Assert.AreEqual(6, world.EntriesToFirstDistrict);
        Assert.AreEqual(6, world.EntriesToSecondDistrict, "Every bot went on to the second district.");
        Assert.AreEqual(6, world.ChannelChanges, "Each asked for its channel once, in the first district.");
        Assert.IsGreaterThan(0, Total(stats, e => e.MoveDelivery).Count, "They hear each other in the new one.");
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task BotsTurnedAwayAtTheDistrictAreCountedAsFailedAndTheRunGoesOn()
    {
        await using var world = FakeWorld.Start();

        world.TurnAwayFromDistrict = true;

        var options = Options(world, 8);

        var (stats, fleet, _) = await RunAsync(options, TestContext.CancellationToken);

        Assert.AreEqual(8, stats.Started);
        Assert.AreEqual(8, stats.Failed);
        Assert.AreEqual(0, stats.PeakActive);
        Assert.AreEqual(8, stats.Stage(BotStage.DistrictEnter).Failed);
        Assert.IsTrue(stats.Errors.ContainsKey("district.enter: closed-by-server"), string.Join(", ", stats.Errors.Keys));
        Assert.IsFalse(fleet.Interrupted);
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task WrongPasswordStopsTheBotAtTheLoginWithTheReasonOfTheServer()
    {
        await using var world = FakeWorld.Start();

        var options = Options(world, 3);

        options.Password = "not the password";

        var (stats, _, _) = await RunAsync(options, TestContext.CancellationToken);

        Assert.AreEqual(3, stats.Stage(BotStage.Login).Failed);
        Assert.AreEqual(0, stats.Stage(BotStage.GateList).Succeeded);
        Assert.IsTrue(stats.Errors.ContainsKey("auth.login: refused-WrongUsernameOrPassword"), string.Join(", ", stats.Errors.Keys));
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task RefusedConnectionIsCountedUnderItsStep()
    {
        var options = new RunOptions
        {
            Bots = 4,
            RampUp = 100,
            Warmup = TimeSpan.Zero,
            Duration = TimeSpan.FromSeconds(1),
            AuthPort = 1, // Nothing listens on it.
            StageTimeout = TimeSpan.FromSeconds(2)
        };

        var (stats, _, _) = await RunAsync(options, TestContext.CancellationToken);

        Assert.AreEqual(4, stats.Failed);
        Assert.AreEqual(4, stats.Stage(BotStage.Login).Failed);
        Assert.IsTrue(stats.Errors.ContainsKey("auth.login: connect-refused"), string.Join(", ", stats.Errors.Keys));
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task BotsThatTheServerDropsAreCountedAsLostAndTheRunFinishes()
    {
        await using var world = FakeWorld.Start();

        var options = Options(world, 6);

        options.Duration = TimeSpan.FromSeconds(3);

        var stats = new RunStats();
        var context = new RunContext { Options = options, Stats = stats };
        var fleet = new Fleet(context);

        var run = fleet.RunAsync(TestContext.CancellationToken);

        while (stats.Active < 6)
        {
            await Task.Delay(50, TestContext.CancellationToken);
        }

        await Task.Delay(500, TestContext.CancellationToken);

        world.DropPlayers();

        await run;

        Assert.AreEqual(6, stats.Lost);
        Assert.AreEqual(0, stats.Active);
        Assert.IsTrue(stats.Errors.ContainsKey("session: closed-by-server"), string.Join(", ", stats.Errors.Keys));
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task StoppingTheRunEndsItAtOnceAndKeepsWhatWasCollected()
    {
        await using var world = FakeWorld.Start();

        var options = Options(world, 6);

        options.Duration = TimeSpan.FromMinutes(5);

        var stats = new RunStats();
        var context = new RunContext { Options = options, Stats = stats };
        var fleet = new Fleet(context);

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);

        var run = fleet.RunAsync(stop.Token);

        while (stats.MeasuredSeconds < 1)
        {
            await Task.Delay(50, TestContext.CancellationToken);
        }

        await stop.CancelAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(15), TestContext.CancellationToken);

        Assert.IsTrue(fleet.Interrupted);
        Assert.AreEqual(RunPhase.Done, fleet.Phase);
        Assert.AreEqual(0, stats.Active, "Every bot has left.");
        Assert.AreEqual(0, stats.Lost, "Leaving at the end is not a loss.");
        Assert.IsGreaterThan(0, Total(stats, e => e.MoveEcho).Count);
        Assert.IsLessThan(10d, stats.MeasuredSeconds);
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task PingAnswersCarryTheDelayOfTheServer()
    {
        await using var world = FakeWorld.Start();

        world.PongDelay = TimeSpan.FromMilliseconds(60);

        var (stats, _, _) = await RunAsync(Options(world, 4), TestContext.CancellationToken);

        var ping = Total(stats, e => e.Ping);
        var echo = Total(stats, e => e.MoveEcho);

        Assert.IsGreaterThan(20, ping.Count);
        Assert.IsGreaterThanOrEqualTo(60d, ping.P50Ms, "The answer cannot come before the server sent it.");
        Assert.IsLessThan(500d, ping.P99Ms);
        Assert.IsLessThan(ping.P50Ms, echo.P50Ms, "A move is answered at once, the ping 60 ms later.");
    }

    [TestMethod]
    [Timeout(90_000)]
    public async Task WholeRunThroughTheCommandLineWritesTheReportAndTheFiles()
    {
        await using var world = FakeWorld.Start();

        var directory = Path.Combine(Path.GetTempPath(), "loadtest-" + Guid.NewGuid().ToString("N"));

        try
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            using var progress = new StringWriter();

            var code = await CommandDispatcher.RunAsync(
                [
                    "run",
                    "--bots", "8",
                    "--ramp-up", "100",
                    "--warmup", "300ms",
                    "--duration", "2s",
                    "--move-rate", "10",
                    "--chat-rate", "1",
                    "--maze-share", "0.25",
                    "--maze-delay", "200ms",
                    "--skill-rate", "5",
                    "--auth-port", world.AuthPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "--account-prefix", world.Prefix,
                    "--password", world.Password,
                    "--progress", "1s",
                    "--output", directory
                ],
                output,
                error,
                progress,
                TestContext.CancellationToken);

            Assert.AreEqual(0, code, error + Environment.NewLine + output);

            var markdown = output.ToString();

            StringAssert.Contains(markdown, "move.echo");
            StringAssert.Contains(markdown, "move.delivery");
            StringAssert.Contains(markdown, "ping.after-move");
            StringAssert.Contains(markdown, "## The bot itself");
            StringAssert.Contains(markdown, "district.enter");
            StringAssert.Contains(progress.ToString(), "Report written to");

            Assert.AreEqual(markdown, File.ReadAllText(Path.Combine(directory, "report.md")));

            using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "report.json")));
            var root = json.RootElement;

            Assert.AreEqual(8, root.GetProperty("bots").GetProperty("requested").GetInt32());
            Assert.AreEqual(8, root.GetProperty("bots").GetProperty("reachedTheWorld").GetInt64());
            Assert.IsGreaterThan(0, root.GetProperty("latency").GetProperty("move.echo").GetProperty("count").GetInt64());
            Assert.IsGreaterThan(0d, root.GetProperty("latency").GetProperty("move.delivery").GetProperty("p99Ms").GetDouble());
        }

        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
