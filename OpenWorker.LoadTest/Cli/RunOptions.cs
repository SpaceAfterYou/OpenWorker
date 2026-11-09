using System.Globalization;
using OpenWorker.LoadTest.Bots;

namespace OpenWorker.LoadTest.Cli;

/// <summary>
/// What a run of the bots is told, from the command line. The defaults are a small run against the servers of
/// <c>compose.yaml</c> on this machine.
/// </summary>
internal sealed class RunOptions
{
    // The scenario.

    public int Bots { get; set; } = 100;

    /// <summary>
    /// Bots started per second.
    /// </summary>
    public double RampUp { get; set; } = 20;

    /// <summary>
    /// How long the latency is measured, from the end of the warm-up.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The time after the last bot has started that is not measured, so the entry of the bots does not count.
    /// </summary>
    public TimeSpan Warmup { get; set; } = TimeSpan.FromSeconds(5);

    public double MoveRate { get; set; } = 5;

    public double ChatRate { get; set; } = 0.05;

    public double MazeShare { get; set; } = 0.1;

    public short MazeLocation { get; set; } = 21311;

    /// <summary>
    /// How long a bot that goes to a maze plays in the district first; each bot adds up to as much again, so they do not leave together.
    /// </summary>
    public TimeSpan MazeDelay { get; set; } = TimeSpan.FromSeconds(3);

    public double SkillRate { get; set; } = 1;

    /// <summary>
    /// The skill a bot uses in a maze; zero takes the first one the character has learned.
    /// </summary>
    public int Skill { get; set; }

    /// <summary>
    /// Every n-th move is followed by a ping that measures how long the server takes to handle it; zero sends none.
    /// </summary>
    public int ProbeEvery { get; set; } = 1;

    // The target.

    public string AuthHost { get; set; } = "127.0.0.1";

    public int AuthPort { get; set; } = 10000;

    /// <summary>
    /// When set, every redirect of the servers is followed to this host, whatever address the server gave.
    /// </summary>
    public string? RedirectHost { get; set; }

    public int Gate { get; set; }

    /// <summary>
    /// The channel the bots ask for once they are in a district; zero leaves them where the server put them.
    /// </summary>
    public int Channel { get; set; }

    /// <summary>
    /// When above zero, bot n asks for channel 1 + n / this once it is in a district, so the channels fill up in order
    /// with this many bots each, the way a district puts a player into the first channel that is not full.
    /// </summary>
    public int ChannelFill { get; set; }

    /// <summary>
    /// The district the bots travel to after they enter the first one; zero keeps them in it.
    /// </summary>
    public int District { get; set; }

    public string AccountPrefix { get; set; } = "lt";

    public int AccountOffset { get; set; }

    public string Password { get; set; } = "loadtest";

    public TimeSpan StageTimeout { get; set; } = TimeSpan.FromSeconds(15);

    // The output.

    public string? Output { get; set; }

    public TimeSpan Progress { get; set; } = TimeSpan.FromSeconds(5);

    // Watching the servers.

    public bool Docker { get; set; }

    public List<string> DockerNames { get; } = [];

    public string DockerPrefix { get; set; } = "OpenWorker-";

    public List<(string Name, int Pid)> Pids { get; } = [];

    public TimeSpan SampleInterval { get; set; } = TimeSpan.FromSeconds(5);

    public string? Prometheus { get; set; }

    public Dictionary<string, string> PrometheusMetrics { get; } = new(StringComparer.Ordinal);

    public List<(string Label, string Query)> PrometheusQueries { get; } = [];

    public string PrometheusGroupBy { get; set; } = "job";

    /// <summary>
    /// What is wrong with the options taken together, or nothing. Each option is checked on its own as it is read.
    /// </summary>
    public IEnumerable<string> Validate()
    {
        if (!AccountNames.Fits(AccountPrefix, AccountOffset, Bots))
        {
            yield return $"The account names '{AccountPrefix}' + a number do not fit the {AccountNames.MaxLength} characters an account name has; " +
                         "use a shorter --account-prefix.";
        }

        if (AccountPrefix.Length == 0 || !AccountPrefix.All(char.IsAsciiLetterOrDigit))
        {
            yield return "--account-prefix must be letters and digits.";
        }

        if (MazeShare > 0 && SkillRate > 0 && MazeLocation <= 0)
        {
            yield return "--maze-location must be a maze of the client data.";
        }

        if (Channel > short.MaxValue)
        {
            yield return "--channel is too big.";
        }

        if (Channel > 0 && ChannelFill > 0)
        {
            yield return "--channel and --channel-fill cannot be given together.";
        }
    }

    public static IReadOnlyList<OptionSpec<RunOptions>> Specs { get; } = BuildSpecs();

    private static OptionSpec<RunOptions>[] BuildSpecs()
    {
        const string scenario = "Scenario";
        const string target = "Target";
        const string output = "Output";
        const string servers = "Watching the servers (all optional)";

        return
        [
            Whole(scenario, "bots", "Number of bots", 100, 1, 100_000, (o, v) => o.Bots = v),
            Real(scenario, "ramp-up", "per-second", "Bots started per second", 20, 0.1, 10_000, (o, v) => o.RampUp = v),
            Time(scenario, "duration", "How long the latency is measured, after the warm-up", TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1), TimeSpan.FromHours(24), (o, v) => o.Duration = v),
            Time(scenario, "warmup", "Time after the last bot has started that is not measured", TimeSpan.FromSeconds(5), TimeSpan.Zero, TimeSpan.FromHours(1), (o, v) => o.Warmup = v),
            Real(scenario, "move-rate", "per-second", "Moves per second of every bot", 5, 0, 60, (o, v) => o.MoveRate = v),
            Real(scenario, "chat-rate", "per-second", "Chat lines per second of every bot; the server lets one person say 5 in 3 s", 0.05, 0, 1.5, (o, v) => o.ChatRate = v),
            Real(scenario, "maze-share", "0..1", "Share of the bots that enter a maze and fight", 0.1, 0, 1, (o, v) => o.MazeShare = v),
            Whole(scenario, "maze-location", "Maze to enter, a location of the maze table of the client", 21311, 1, short.MaxValue, (o, v) => o.MazeLocation = (short)v),
            Time(scenario, "maze-delay", "How long a bot that goes to a maze plays in the district first; each adds up to as much again", TimeSpan.FromSeconds(3), TimeSpan.Zero, TimeSpan.FromHours(1), (o, v) => o.MazeDelay = v),
            Real(scenario, "skill-rate", "per-second", "Skills used per second by a bot in a maze", 1, 0, 30, (o, v) => o.SkillRate = v),
            Whole(scenario, "skill", "Skill a bot uses in a maze; 0 takes the first one its character has learned", 0, 0, int.MaxValue, (o, v) => o.Skill = v),
            Whole(scenario, "probe-every", "Send a ping behind every n-th move to measure how long the server takes to handle it; 0 sends none", 1, 0, 1000, (o, v) => o.ProbeEvery = v),

            Text(target, "auth-host", "host", "Host of the auth server", "127.0.0.1", (o, v) => o.AuthHost = v),
            Whole(target, "auth-port", "Port of the auth server", 10000, 1, 65535, (o, v) => o.AuthPort = v),
            Text(target, "redirect-host", "host", "Connect to this host whatever address a server sends the bot to (the servers answer 127.0.0.1 by default)", null, (o, v) => o.RedirectHost = v),
            Whole(target, "gate", "Gate to enter; 0 takes the first one that is online", 0, 0, short.MaxValue, (o, v) => o.Gate = v),
            Whole(target, "channel", "Channel to ask for after entering a district; 0 stays in the one the server chose (a channel holds 65 players)", 0, 0, short.MaxValue, (o, v) => o.Channel = v),
            Whole(target, "channel-fill", "Fill the channels in order with this many bots each: bot n asks for channel 1 + n / this after entering a district; 0 does not", 0, 0, 10_000, (o, v) => o.ChannelFill = v),
            Whole(target, "district", "Location of the district to travel to after the first one; 0 stays in it", 0, 0, short.MaxValue, (o, v) => o.District = v),
            Text(target, "account-prefix", "text", "Accounts are this text and a number: lt00000, lt00001, ...; create them with the seed command", "lt", (o, v) => o.AccountPrefix = v),
            Whole(target, "account-offset", "Number of the first account", 0, 0, 10_000_000, (o, v) => o.AccountOffset = v),
            Text(target, "password", "text", "Password of the accounts", "loadtest", (o, v) => o.Password = v),
            Time(target, "stage-timeout", "How long a bot waits for an answer or a connection before it gives up", TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(100), TimeSpan.FromHours(1), (o, v) => o.StageTimeout = v),

            Text(output, "output", "directory", "Directory for report.json and report.md", "TestResults/loadtest/<time>", (o, v) => o.Output = v),
            Time(output, "progress", "How often a line of progress is written to stderr; 0 writes none", TimeSpan.FromSeconds(5), TimeSpan.Zero, TimeSpan.FromHours(1), (o, v) => o.Progress = v),

            Switch(servers, "docker-stats", "Sample CPU and memory of the containers with 'docker stats'", o => o.Docker = true),
            Text(servers, "docker-names", "a,b", "Containers to sample; by default every running container whose name starts with the prefix", null, (o, v) => o.DockerNames.AddRange(v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))),
            Text(servers, "docker-prefix", "text", "Name prefix of the containers sampled when no names are given", "OpenWorker-", (o, v) => o.DockerPrefix = v),
            PidsOption(servers),
            Time(servers, "sample-interval", "How often the servers and the bot itself are sampled", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromHours(1), (o, v) => o.SampleInterval = v),
            Text(servers, "prometheus", "url", "Prometheus to ask for the loop and handler metrics after the run, e.g. http://localhost:9090", null, (o, v) => o.Prometheus = v),
            Checked(servers, "prom-metric", "name=metric", "Name of a metric as Prometheus has it, e.g. worldloop-queue=worldloop_queue_length; may be given more than once", null, ApplyMetric),
            Checked(servers, "prom-query", "label=promql", "A query of your own, {window} is the length of the run, e.g. 'gc=rate(process_runtime_dotnet_gc_collections_count_total[{window}])'; may be given more than once", null, ApplyQuery),
            Text(servers, "prom-group-by", "label", "Label to give a result per server by; empty gives one result for all of them", "job", (o, v) => o.PrometheusGroupBy = v)
        ];
    }

    private static string? ApplyMetric(RunOptions options, string value)
    {
        var split = value.Split('=', 2);

        if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
        {
            return "expected name=metric.";
        }

        options.PrometheusMetrics[split[0]] = split[1];

        return null;
    }

    private static string? ApplyQuery(RunOptions options, string value)
    {
        var split = value.Split('=', 2);

        if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
        {
            return "expected label=promql.";
        }

        options.PrometheusQueries.Add((split[0], split[1]));

        return null;
    }

    private static OptionSpec<RunOptions> PidsOption(string group)
    {
        return new OptionSpec<RunOptions>
        {
            Name = "pids",
            ValueName = "name=pid,name=pid",
            Group = group,
            Description = "Sample CPU and memory of processes through /proc (Linux), e.g. district=1234,maze=1235",
            Apply = (options, value) =>
            {
                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var split = part.Split('=', 2);
                    var named = split.Length == 2;

                    if (!int.TryParse(named ? split[1] : split[0], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) || pid <= 0)
                    {
                        return $"'{part}' is not name=pid.";
                    }

                    options.Pids.Add((named ? split[0] : "pid" + pid.ToString(CultureInfo.InvariantCulture), pid));
                }

                return null;
            }
        };
    }

    private static OptionSpec<RunOptions> Whole(string group, string name, string description, int @default, int min, int max, Action<RunOptions, int> set)
    {
        return new OptionSpec<RunOptions>
        {
            Name = name,
            ValueName = "n",
            Group = group,
            Description = description,
            DefaultText = @default.ToString(CultureInfo.InvariantCulture),
            Apply = (options, value) =>
            {
                var problem = OptionParser.ParseInt(value, min, max, out var result);

                if (problem is null)
                {
                    set(options, result);
                }

                return problem;
            }
        };
    }

    private static OptionSpec<RunOptions> Real(string group, string name, string valueName, string description, double @default, double min, double max, Action<RunOptions, double> set)
    {
        return new OptionSpec<RunOptions>
        {
            Name = name,
            ValueName = valueName,
            Group = group,
            Description = description,
            DefaultText = @default.ToString(CultureInfo.InvariantCulture),
            Apply = (options, value) =>
            {
                var problem = OptionParser.ParseDouble(value, min, max, out var result);

                if (problem is null)
                {
                    set(options, result);
                }

                return problem;
            }
        };
    }

    private static OptionSpec<RunOptions> Time(string group, string name, string description, TimeSpan @default, TimeSpan min, TimeSpan max, Action<RunOptions, TimeSpan> set)
    {
        return new OptionSpec<RunOptions>
        {
            Name = name,
            ValueName = "time",
            Group = group,
            Description = description,
            DefaultText = Durations.Format(@default),
            Apply = (options, value) =>
            {
                var problem = OptionParser.ParseDuration(value, min, max, out var result);

                if (problem is null)
                {
                    set(options, result);
                }

                return problem;
            }
        };
    }

    /// <summary>
    /// An option whose value can be wrong in a way only its own parser knows: it returns the reason, or null.
    /// </summary>
    private static OptionSpec<RunOptions> Checked(string group, string name, string valueName, string description, string? @default, Func<RunOptions, string, string?> apply)
    {
        return new OptionSpec<RunOptions>
        {
            Name = name,
            ValueName = valueName,
            Group = group,
            Description = description,
            DefaultText = @default,
            Apply = apply
        };
    }

    private static OptionSpec<RunOptions> Text(string group, string name, string valueName, string description, string? @default, Action<RunOptions, string> set)
    {
        return Checked(group, name, valueName, description, @default, (options, value) =>
        {
            set(options, value);

            return null;
        });
    }

    private static OptionSpec<RunOptions> Switch(string group, string name, string description, Action<RunOptions> set)
    {
        return new OptionSpec<RunOptions>
        {
            Name = name,
            Group = group,
            Description = description,
            Apply = (options, _) =>
            {
                set(options);

                return null;
            }
        };
    }
}
