using OpenWorker.LoadTest.Commands;

namespace OpenWorker.LoadTest.Cli;

/// <summary>
/// Picks the command from the arguments and runs it. It writes to the streams it is given and never touches the
/// console, so a test can run it and read what it says.
/// </summary>
internal static class CommandDispatcher
{
    private const string Usage = "Usage: OpenWorker.LoadTest <run|seed> [options]   (a bare list of options means run)";

    public static async Task<int> RunAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error, TextWriter progress, CancellationToken cancellation)
    {
        if (args.Count == 0 || args[0] is "help" or "-h" or "--help" or "-?")
        {
            WriteOverview(output);

            return 0;
        }

        switch (args[0])
        {
            case "seed":
                return await SeedAsync(args.Skip(1).ToArray(), output, error, cancellation);

            case "run":
                return await RunBotsAsync(args.Skip(1).ToArray(), output, error, progress, cancellation);

            default:
                if (args[0].StartsWith("--", StringComparison.Ordinal))
                {
                    return await RunBotsAsync(args, output, error, progress, cancellation);
                }

                error.WriteLine($"Unknown command '{args[0]}'.");
                error.WriteLine(Usage);

                return 2;
        }
    }

    private static async Task<int> RunBotsAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error, TextWriter progress, CancellationToken cancellation)
    {
        var options = new RunOptions();
        var errors = OptionParser.Parse(args, RunOptions.Specs, options, out var help);

        if (help)
        {
            OptionParser.WriteHelp(
                output,
                "Usage: OpenWorker.LoadTest run [options]",
                "Runs bots against the servers and reports the latency of movement, the CPU and memory of the servers, and the lag of the loop.",
                RunOptions.Specs);

            return 0;
        }

        errors.AddRange(options.Validate());

        if (errors.Count > 0)
        {
            return Refuse(error, errors, "Usage: OpenWorker.LoadTest run [options]   (--help lists them)");
        }

        return await RunCommand.RunAsync(options, output, progress, cancellation);
    }

    private static async Task<int> SeedAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error, CancellationToken cancellation)
    {
        var options = new SeedOptions();
        var errors = OptionParser.Parse(args, SeedOptions.Specs, options, out var help);

        if (help)
        {
            OptionParser.WriteHelp(
                output,
                "Usage: OpenWorker.LoadTest seed [options]",
                "Creates the accounts the bots sign in with, straight in the database. Accounts that exist are left alone.",
                SeedOptions.Specs);

            return 0;
        }

        errors.AddRange(options.Validate());

        if (errors.Count > 0)
        {
            return Refuse(error, errors, "Usage: OpenWorker.LoadTest seed [options]   (--help lists them)");
        }

        return await SeedCommand.RunAsync(options, output, cancellation);
    }

    private static int Refuse(TextWriter error, List<string> errors, string usage)
    {
        foreach (var message in errors)
        {
            error.WriteLine(message);
        }

        error.WriteLine(usage);

        return 2;
    }

    private static void WriteOverview(TextWriter output)
    {
        output.WriteLine(Usage);
        output.WriteLine();
        output.WriteLine("Bots for the servers of OpenWorker: each signs in, enters a district, walks, talks, and a share of them fights in a maze.");
        output.WriteLine("The report has the latency of movement, the CPU and memory of the servers, and the lag of the world loop.");
        output.WriteLine();
        output.WriteLine("  run     Run the bots and write the report (OpenWorker.LoadTest run --help)");
        output.WriteLine("  seed    Create the accounts the bots sign in with (OpenWorker.LoadTest seed --help)");
        output.WriteLine();
        output.WriteLine("Ctrl+C stops the run early and still writes the report of what was collected.");
    }
}
