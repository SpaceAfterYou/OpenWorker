using System.Globalization;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.LoadTest.Bots;
using OpenWorker.LoadTest.Cli;
using OpenWorker.LoadTest.Monitoring;
using OpenWorker.LoadTest.Protocol;
using OpenWorker.LoadTest.Reporting;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Test;

/// <summary>
/// The parts of the tool around the protocol: the statistics, the framing, the command line, the samplers and the report.
/// </summary>
[TestClass]
public sealed class ToolingTest
{
    [TestMethod]
    public void HistogramPercentilesAreWithinThreePercentAndNeverBelowTheTrueValue()
    {
        var histogram = new LatencyHistogram();

        // 1 ms to 100 ms in steps of 10 µs: 9901 values, uniform.
        for (var micro = 1000; micro <= 100_000; micro += 10)
        {
            histogram.Record(micro);
        }

        Assert.AreEqual(9901, histogram.Count);
        Assert.AreEqual(100_000, histogram.MaxMicroseconds);

        foreach (var (share, truth) in new[] { (0.50, 50_500d), (0.95, 95_050d), (0.99, 99_010d) })
        {
            var value = histogram.Percentile(share);

            Assert.IsGreaterThanOrEqualTo(truth, value, $"p{share} is never reported below the real one.");
            Assert.IsLessThanOrEqualTo(truth * 1.035, value, $"p{share} is within the width of a bucket.");
        }

        Assert.AreEqual(50_500d, histogram.MeanMicroseconds, 1d);
    }

    [TestMethod]
    public void HistogramKeepsSmallValuesExactAndTheMaximumExact()
    {
        var histogram = new LatencyHistogram();

        histogram.Record(7);
        histogram.Record(7);
        histogram.Record(31);
        histogram.Record(1_234_567);

        Assert.AreEqual(7, histogram.Percentile(0.5));
        Assert.AreEqual(31, histogram.Percentile(0.75));
        Assert.AreEqual(1_234_567, histogram.Percentile(1.0), "The top percentile is the exact maximum, not the top of its bucket.");
        Assert.AreEqual(0, new LatencyHistogram().Percentile(0.99));
    }

    [TestMethod]
    public void HistogramsAddUp()
    {
        var first = new LatencyHistogram();
        var second = new LatencyHistogram();

        for (var i = 1; i <= 100; i++)
        {
            first.Record(i * 100);
            second.Record(i * 100 + 50_000);
        }

        var total = new LatencyHistogram();

        total.Add(first);
        total.Add(second);
        total.Add(new LatencyHistogram());

        Assert.AreEqual(200, total.Count);
        Assert.AreEqual(60_000, total.MaxMicroseconds);
        Assert.IsGreaterThanOrEqualTo(10_000, total.Percentile(0.5));
        Assert.IsLessThan(10_500, total.Percentile(0.5));
    }

    [TestMethod]
    public void SummaryIsInMilliseconds()
    {
        var histogram = new LatencyHistogram();

        histogram.Record(2500);

        var summary = LatencySummary.Of(histogram);

        Assert.AreEqual(1, summary.Count);
        Assert.AreEqual(2.5, summary.MaxMs);
        Assert.AreEqual(2.5, summary.P99Ms);
        Assert.AreSame(LatencySummary.Empty, LatencySummary.Of(new LatencyHistogram()));
    }

    [TestMethod]
    public void CipherAgreesWithTheEncryptionOfTheServerForEveryVersionAndLength()
    {
        var random = new Random(5);

        for (ushort version = 0; version <= FrameCipher.MaxVersion; version++)
        {
            for (var length = 0; length < 40; length++)
            {
                var plain = new byte[length];

                random.NextBytes(plain);

                var ours = (byte[])plain.Clone();
                var theirs = (byte[])plain.Clone();

                FrameCipher.Apply(ours, version);
                MessageEncryption.Exchange(theirs.AsSpan(), version);

                CollectionAssert.AreEqual(theirs, ours, $"version {version}, {length} bytes");

                FrameCipher.Apply(ours, version);

                CollectionAssert.AreEqual(plain, ours, "Applying it twice gives the text back.");
            }
        }

        Assert.IsFalse(FrameCipher.IsKnown(FrameCipher.MaxVersion + 1));
        Assert.Throws<InvalidDataException>(() => FrameCipher.Apply(new byte[3], FrameCipher.MaxVersion + 1));
    }

    [TestMethod]
    public void FramesAreFoundWhereverTheBytesAreCut()
    {
        var frames = new[]
        {
            Wire.ServerFrame(Opcodes.Ping, w => w.Write(1L)),
            Wire.ServerFrame(Opcodes.ChatNormal, w => w.Write(new byte[3000])),
            Wire.ServerFrame(Opcodes.Ping, w => w.Write(3L)),
            Wire.ServerFrame(Opcodes.PersonList, w => w.Write(new byte[60_000]))
        };

        var stream = frames.SelectMany(e => e).ToArray();

        foreach (var chunk in new[] { 1, 2, 5, 7, 100, 1500, 4096, stream.Length })
        {
            using var buffer = new FrameBuffer();

            var seen = new List<(ushort Opcode, int Length)>();
            var offset = 0;

            while (offset < stream.Length)
            {
                var memory = buffer.ReceiveMemory();
                var count = Math.Min(Math.Min(chunk, memory.Length), stream.Length - offset);

                stream.AsMemory(offset, count).CopyTo(memory);
                buffer.Advance(count);

                offset += count;

                while (buffer.TryReadFrame(out var body))
                {
                    seen.Add((BitConverter.ToUInt16(body), body.Length));
                }
            }

            Assert.HasCount(4, seen, $"chunks of {chunk}");
            Assert.AreEqual((Opcodes.Ping, 10), seen[0]);
            Assert.AreEqual((Opcodes.ChatNormal, 3002), seen[1]);
            Assert.AreEqual((Opcodes.Ping, 10), seen[2]);
            Assert.AreEqual((Opcodes.PersonList, 60_002), seen[3]);
            Assert.AreEqual(0, buffer.Buffered);
        }
    }

    [TestMethod]
    public void BrokenFramesAreErrorsOfTheProtocol()
    {
        using var shortLength = new FrameBuffer();

        shortLength.ReceiveMemory().Span[..5].Clear();
        shortLength.Advance(5);

        Assert.Throws<InvalidDataException>(() => shortLength.TryReadFrame(out _));

        using var unknownVersion = new FrameBuffer();

        var header = unknownVersion.ReceiveMemory().Span;

        header[0] = 99;
        header[2] = 9;
        unknownVersion.Advance(5);

        Assert.Throws<InvalidDataException>(() => unknownVersion.TryReadFrame(out _));
    }

    [TestMethod]
    public void MovePositionsTellTheirPointOfTheCircleAtTheCoordinatesOfADistrict()
    {
        foreach (var (cx, cy) in new[] { (10228.605f, 10058.951f), (15000f, 15000f), (0f, 0f), (-500f, 20000.5f), (30000f, 30000f) })
        {
            for (var index = 0; index < MoveTrack.Points; index++)
            {
                var (x, y) = MoveTrack.PointAt(cx, cy, index);

                Assert.AreEqual(index, MoveTrack.IndexOf(cx, cy, x, y), $"({cx}, {cy}) point {index}");
            }
        }

        Assert.AreEqual(-1, MoveTrack.IndexOf(100f, 100f, 100f, 100f), "The center is not a point.");
        Assert.AreEqual(-1, MoveTrack.IndexOf(100f, 100f, 400f, 100f), "Nor is a place far away.");
        Assert.AreEqual(MoveTrack.PointAt(1f, 2f, 3), MoveTrack.PointAt(1f, 2f, 3 + MoveTrack.Points));
    }

    [TestMethod]
    public void HeadingsStayInTheRangeTheServerAccepts()
    {
        for (var index = 0; index < MoveTrack.Points * 2; index++)
        {
            Assert.IsInRange(0f, 360f, MoveTrack.HeadingAt(index));
        }
    }

    [TestMethod]
    public void MazeBotsAreSpreadEvenlyAndTheShareIsKept()
    {
        foreach (var share in new[] { 0d, 0.1, 0.25, 1d / 3, 0.5, 1d })
        {
            var count = Enumerable.Range(0, 1000).Count(i => RunContext.GoesToMaze(i, share));

            Assert.AreEqual(Math.Floor(1000 * share), count, 1d, $"share {share}");
        }

        Assert.IsFalse(RunContext.GoesToMaze(0, 0.1));
        Assert.IsTrue(RunContext.GoesToMaze(9, 0.1));
    }

    [TestMethod]
    public void ActionsAreScheduledAtTheirRateAndALateBotSkipsWhatItMissed()
    {
        Assert.AreEqual(1100L, Bot.Advance(1000, 100, 1050));
        Assert.AreEqual(long.MaxValue, Bot.Advance(1000, 0, 1050));

        // Three periods late: the next one is a period from now, not a burst to catch up.
        Assert.AreEqual(1400L, Bot.Advance(1000, 100, 1300));
    }

    [TestMethod]
    [DataRow("500ms", 0.5)]
    [DataRow("30s", 30d)]
    [DataRow("30", 30d)]
    [DataRow("5m", 300d)]
    [DataRow("1h", 3600d)]
    [DataRow("1m30s", 90d)]
    [DataRow("1.5s", 1.5)]
    public void DurationsAreReadWithTheirUnits(string text, double seconds)
    {
        Assert.IsTrue(Durations.TryParse(text, out var value));
        Assert.AreEqual(seconds, value.TotalSeconds, 1e-9);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("abc")]
    [DataRow("5x")]
    [DataRow("1m30")]
    [DataRow("-5s")]
    public void BrokenDurationsAreRefused(string text)
    {
        Assert.IsFalse(Durations.TryParse(text, out _));
    }

    [TestMethod]
    public void DurationsAreWrittenSoTheyCanBeReadBack()
    {
        foreach (var seconds in new[] { 0.5, 1d, 59d, 60d, 90d, 3600d, 3725.5 })
        {
            var text = Durations.Format(TimeSpan.FromSeconds(seconds));

            Assert.IsTrue(Durations.TryParse(text, out var back), text);
            Assert.AreEqual(seconds, back.TotalSeconds, 1e-9, text);
        }
    }

    [TestMethod]
    public void CommandLineIsReadWithTheInvariantCultureAndEveryMistakeIsReported()
    {
        var previous = CultureInfo.CurrentCulture;

        try
        {
            // A decimal comma must not change how "1.5" is read.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var options = new RunOptions();
            var errors = OptionParser.Parse(
                ["--bots=250", "--move-rate", "2.5", "--duration", "2m", "--redirect-host", "host", "--docker-stats", "--pids", "a=12,13", "--prom-metric", "worldloop-queue=q"],
                RunOptions.Specs,
                options,
                out var help);

            Assert.IsEmpty(errors);
            Assert.IsFalse(help);
            Assert.AreEqual(250, options.Bots);
            Assert.AreEqual(2.5, options.MoveRate);
            Assert.AreEqual(TimeSpan.FromMinutes(2), options.Duration);
            Assert.AreEqual("host", options.RedirectHost);
            Assert.IsTrue(options.Docker);
            Assert.AreEqual(("a", 12), options.Pids[0]);
            Assert.AreEqual(("pid13", 13), options.Pids[1]);
            Assert.AreEqual("q", options.PrometheusMetrics["worldloop-queue"]);

            var bad = OptionParser.Parse(["--bots", "x", "--move-rate", "2,5", "--nope", "--maze-share", "2", "stray", "--duration"], RunOptions.Specs, new RunOptions(), out _);

            Assert.HasCount(6, bad);
        }

        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void HelpListsEveryOptionWithItsDefault()
    {
        using var output = new StringWriter();

        OptionParser.WriteHelp(output, "usage", "summary", RunOptions.Specs);

        var help = output.ToString();

        foreach (var spec in RunOptions.Specs)
        {
            StringAssert.Contains(help, "--" + spec.Name);
        }

        StringAssert.Contains(help, "(default: 100)");
        StringAssert.Contains(help, "(default: 1m)");
    }

    [TestMethod]
    public void OptionsTakenTogetherAreChecked()
    {
        Assert.IsEmpty(new RunOptions().Validate());
        Assert.IsNotEmpty(new RunOptions { AccountPrefix = "toolongprefix" }.Validate());
        Assert.IsNotEmpty(new RunOptions { AccountPrefix = "a b" }.Validate());

        // 12 characters hold a prefix of 7 and a number of 5 digits, not a prefix of 8.
        Assert.IsTrue(AccountNames.Fits("lt", 0, 100_000));
        Assert.IsTrue(AccountNames.Fits("sevench", 0, 100_000));
        Assert.IsFalse(AccountNames.Fits("eightch8", 0, 100_000));
        Assert.AreEqual("lt00042", AccountNames.Format("lt", 42));
        Assert.AreEqual("02-4C-54-00-01-02", AccountNames.MacAddress(258));
    }

    [TestMethod]
    public void DockerStatsLinesAreRead()
    {
        const string line = """{"BlockIO":"0B / 0B","CPUPerc":"12.34%","Container":"abc","ID":"abc","MemPerc":"0.12%","MemUsage":"18.5MiB / 15.52GiB","Name":"OpenWorker-DistrictServer-RoccoTown","NetIO":"1kB / 2kB","PIDs":"31"}""";

        Assert.IsTrue(DockerStatsSampler.TryParseLine(line, out var name, out var cpu, out var memory));
        Assert.AreEqual("OpenWorker-DistrictServer-RoccoTown", name);
        Assert.AreEqual(12.34, cpu);
        Assert.AreEqual((long)(18.5 * 1024 * 1024), memory);

        Assert.IsFalse(DockerStatsSampler.TryParseLine("""{"Name":"x","CPUPerc":"--","MemUsage":"-- / --"}""", out _, out _, out _));
        Assert.IsFalse(DockerStatsSampler.TryParseLine("not json", out _, out _, out _));
        Assert.IsFalse(DockerStatsSampler.TryParseLine("""{"Name":"x"}""", out _, out _, out _));
    }

    [TestMethod]
    [DataRow("0B", 0L)]
    [DataRow("512B", 512L)]
    [DataRow("340kB", 340_000L)]
    [DataRow("1.5MiB", 1_572_864L)]
    [DataRow("2GiB", 2_147_483_648L)]
    [DataRow("3MB", 3_000_000L)]
    public void DockerSizesAreRead(string text, long bytes)
    {
        Assert.IsTrue(DockerStatsSampler.TryParseSize(text, out var value));
        Assert.AreEqual(bytes, value);
    }

    [TestMethod]
    public void ProcStatAndStatusAreRead()
    {
        // The name of the process holds spaces and a parenthesis, and the fields are counted after the last one.
        const string stat = "1234 (my (weird) name) S 1 1234 1234 0 -1 4194560 1000 0 5 0 700 300 0 0 20 0 8 0 100 1000000 5000 18446744073709551615";

        Assert.IsTrue(ProcSampler.TryParseStat(stat, out var ticks));
        Assert.AreEqual(1000L, ticks);
        Assert.IsFalse(ProcSampler.TryParseStat("garbage", out _));

        Assert.IsTrue(ProcSampler.TryParseResidentMemory("Name:\tx\nVmRSS:\t   2048 kB\nThreads:\t3\n", out var bytes));
        Assert.AreEqual(2048L * 1024, bytes);
        Assert.IsFalse(ProcSampler.TryParseResidentMemory("Name:\tx\n", out _));
    }

    [TestMethod]
    public void ProcSamplerReadsThisProcess()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var sampler = new ProcSampler([("self", Environment.ProcessId)]);
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(2500));

        sampler.RunAsync(TimeSpan.FromSeconds(1), System.Diagnostics.Stopwatch.GetTimestamp(), stop.Token).GetAwaiter().GetResult();

        Assert.IsNotEmpty(sampler.Samples);
        Assert.IsGreaterThan(1_000_000L, sampler.Samples[0].MemoryBytes);
        Assert.IsGreaterThanOrEqualTo(0d, sampler.Samples[0].CpuPercent);
    }

    [TestMethod]
    public void PrometheusVectorIsRead()
    {
        const string json = """{"status":"success","data":{"resultType":"vector","result":[{"metric":{"job":"district"},"value":[1700000000.5,"12.5"]},{"metric":{},"value":[1700000000.5,"NaN"]},{"metric":{"job":"maze"},"value":[1700000000.5,"3"]}]}}""";

        var series = PrometheusClient.ParseVector(json, out var problem);

        Assert.IsNull(problem);
        Assert.HasCount(2, series, "A NaN is left out.");
        Assert.AreEqual("district", series[0].Labels["job"]);
        Assert.AreEqual(12.5, series[0].Value);

        PrometheusClient.ParseVector("""{"status":"error","error":"bad query"}""", out problem);

        Assert.AreEqual("bad query", problem);

        PrometheusClient.ParseVector("<html>", out problem);

        StringAssert.Contains(problem, "cannot be read");
    }

    [TestMethod]
    public void PrometheusQueriesUseTheNamesOfTheMetricsFromTheSettings()
    {
        var options = new RunOptions();

        options.PrometheusMetrics["worldloop-queue"] = "my_queue";
        options.PrometheusQueries.Add(("custom", "rate(x[{window}])"));

        var queries = PrometheusClient.BuildQueries(options, TimeSpan.FromSeconds(74.2));

        Assert.IsTrue(queries.Any(e => e.Query == "max by (job) (max_over_time(my_queue[75s]))"));
        Assert.IsTrue(queries.Any(e => e.Query.Contains("histogram_quantile(0.99, sum by (job, le) (rate(worldloop_callback_duration_seconds_bucket[75s])))", StringComparison.Ordinal)));
        Assert.IsTrue(queries.Any(e => e.Label == "custom" && e.Query == "rate(x[75s])"));

        options.PrometheusGroupBy = string.Empty;

        Assert.IsTrue(PrometheusClient.BuildQueries(options, TimeSpan.FromSeconds(10)).Any(e => e.Query == "max (max_over_time(my_queue[10s]))"));
    }

    [TestMethod]
    public async Task PrometheusThatCannotBeReachedSkipsTheStepWithANote()
    {
        using var client = new HttpClient();

        var report = await PrometheusClient.QueryAsync(client, "http://127.0.0.1:1", [("a", "up")], DateTimeOffset.UtcNow, CancellationToken.None);

        StringAssert.Contains(report.Note, "not reachable");
        Assert.IsEmpty(report.Results);

        var bad = await PrometheusClient.QueryAsync(client, "localhost:9090", [("a", "up")], DateTimeOffset.UtcNow, CancellationToken.None);

        StringAssert.Contains(bad.Note, "not an http address");
    }

    [TestMethod]
    public void ReportIsWrittenWithDotsWhateverTheCultureAndHasTheMovementRows()
    {
        var previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

            var stats = new RunStats();
            var bot = new BotMetrics();

            stats.Register(bot);
            stats.BotStarted();
            stats.BotEntered();
            stats.StartMeasuring();

            for (var i = 1; i <= 200; i++)
            {
                bot.MoveEcho.Record(i * 1000 + 250);
                bot.MoveDelivery.Record(i * 2000);
                bot.Ping.Record(i * 500);
            }

            bot.MovesSentWhileMeasured = 100;

            stats.Error("auth.login", "refused-WrongUsernameOrPassword", "The server | said no.");
            stats.StopMeasuring();

            var report = ReportBuilder.Build(new ReportInputs(
                new RunOptions { Bots = 1, MoveRate = 2.5 },
                stats,
                new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero),
                75.5,
                false,
                [new SelfSample(1, 1.25, 100 * 1048576L, 1, 0, 0, 3.5, 20, 0)],
                [],
                null,
                [],
                []));

            using var markdown = new StringWriter();

            MarkdownReport.Write(markdown, report);

            var text = markdown.ToString();

            StringAssert.Contains(text, "| move.echo | 200 |");
            StringAssert.Contains(text, "| move.delivery | 200 |");
            StringAssert.Contains(text, "| ping.after-move | 200 |");
            StringAssert.Contains(text, "75.5 s");
            StringAssert.Contains(text, "2.5 per second");
            StringAssert.Contains(text, "The server / said no.");
            StringAssert.Contains(text, "1.25 cores on average");
            Assert.IsFalse(text.Contains("2,5", StringComparison.Ordinal), "No decimal comma.");

            var json = MarkdownReport.ToJson(report);

            StringAssert.Contains(json, "\"p99Ms\":");
            StringAssert.Contains(json, "\"move.echo\"");
            StringAssert.Contains(json, "2026-11-08T12:00:00+00:00");

            var echo = report.Latency[LatencyNames.MoveEcho];

            Assert.AreEqual(200, echo.Count);
            Assert.AreEqual(200.25, echo.MaxMs, 1e-9);
            Assert.IsGreaterThanOrEqualTo(100.25, echo.P50Ms);
            Assert.IsLessThan(104, echo.P50Ms);
            Assert.AreEqual(4.0, report.Traffic.AverageListeners, 1e-9, "200 echoes and 200 deliveries for 100 moves.");
        }

        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [TestMethod]
    public void ReportWarnsWhenNobodyGotInAndWhenTheBotWasBusy()
    {
        var stats = new RunStats();
        var bot = new BotMetrics();

        stats.Register(bot);
        stats.BotStarted();
        stats.BotFailedToEnter();

        var busy = ReportBuilder.Build(new ReportInputs(
            new RunOptions { Bots = 1 },
            stats,
            DateTimeOffset.UtcNow,
            10,
            false,
            [new SelfSample(1, Environment.ProcessorCount, 1, 0, 0, 0, 0, 1, 0)],
            [],
            null,
            [],
            []));

        Assert.IsTrue(busy.Warnings.Any(e => e.Contains("No bot got into a district", StringComparison.Ordinal)));
        Assert.IsTrue(busy.Warnings.Any(e => e.Contains("The bot used", StringComparison.Ordinal)));
        Assert.IsTrue(busy.Warnings.Any(e => e.Contains("never got into the world", StringComparison.Ordinal)));
    }
}
