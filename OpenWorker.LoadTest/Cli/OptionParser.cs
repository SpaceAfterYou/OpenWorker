using System.Globalization;

namespace OpenWorker.LoadTest.Cli;

/// <summary>
/// One option of a command: its name, what it is for, and how its value is stored in the options of the command.
/// </summary>
internal sealed class OptionSpec<TOptions>
{
    /// <summary>
    /// The name without the dashes.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The placeholder of the value in the help, or null for a switch that takes none.
    /// </summary>
    public string? ValueName { get; init; }

    public required string Group { get; init; }

    public required string Description { get; init; }

    public string? DefaultText { get; init; }

    /// <summary>
    /// Stores the value; returns the reason when it is wrong, otherwise null. A switch gets an empty value.
    /// </summary>
    public required Func<TOptions, string, string?> Apply { get; init; }
}

/// <summary>
/// The command line of the tool: <c>--name value</c>, <c>--name=value</c> and the switches that stand alone. The
/// options a command takes are listed once, and both the parsing and the help are made from that list.
/// </summary>
internal static class OptionParser
{
    /// <summary>
    /// Reads the arguments into <paramref name="target" />. Every mistake is collected, so one run tells all of
    /// them. <paramref name="help" /> is set when the arguments ask for it, whatever else they hold.
    /// </summary>
    public static List<string> Parse<TOptions>(IReadOnlyList<string> args, IReadOnlyList<OptionSpec<TOptions>> specs, TOptions target, out bool help)
    {
        var errors = new List<string>();

        help = false;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            if (arg is "-h" or "--help" or "-?")
            {
                help = true;

                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                errors.Add($"Unexpected argument '{arg}'.");

                continue;
            }

            var name = arg[2..];
            string? inline = null;
            var equals = name.IndexOf('=', StringComparison.Ordinal);

            if (equals >= 0)
            {
                inline = name[(equals + 1)..];
                name = name[..equals];
            }

            var spec = specs.FirstOrDefault(e => e.Name == name);

            if (spec is null)
            {
                errors.Add($"Unknown option '--{name}'.");

                continue;
            }

            string value;

            if (spec.ValueName is null)
            {
                value = string.Empty;
            }

            else if (inline is not null)
            {
                value = inline;
            }

            else if (i + 1 < args.Count)
            {
                value = args[++i];
            }

            else
            {
                errors.Add($"The option '--{name}' needs a value ({spec.ValueName}).");

                continue;
            }

            if (spec.Apply(target, value) is { } problem)
            {
                errors.Add($"The option '--{name}': {problem}");
            }
        }

        return errors;
    }

    public static void WriteHelp<TOptions>(TextWriter writer, string usage, string summary, IReadOnlyList<OptionSpec<TOptions>> specs)
    {
        writer.WriteLine(usage);
        writer.WriteLine();
        writer.WriteLine(summary);

        foreach (var group in specs.GroupBy(e => e.Group))
        {
            writer.WriteLine();
            writer.WriteLine(group.Key + ":");

            foreach (var spec in group)
            {
                var left = spec.ValueName is null ? $"--{spec.Name}" : $"--{spec.Name} <{spec.ValueName}>";
                var text = spec.DefaultText is null ? spec.Description : $"{spec.Description} (default: {spec.DefaultText})";

                writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {left,-34} {text}"));
            }
        }
    }

    public static string? ParseInt(string value, int minimum, int maximum, out int result)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return $"'{value}' is not a whole number.";
        }

        return result < minimum || result > maximum ? $"{result} is out of the range {minimum} to {maximum}." : null;
    }

    public static string? ParseDouble(string value, double minimum, double maximum, out double result)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || !double.IsFinite(result))
        {
            return $"'{value}' is not a number (write the decimal point as a dot).";
        }

        return result < minimum || result > maximum
            ? string.Create(CultureInfo.InvariantCulture, $"{result} is out of the range {minimum} to {maximum}.")
            : null;
    }

    public static string? ParseDuration(string value, TimeSpan minimum, TimeSpan maximum, out TimeSpan result)
    {
        if (!Durations.TryParse(value, out result))
        {
            return $"'{value}' is not a length of time (for example 500ms, 30s, 5m, 1h).";
        }

        return result < minimum || result > maximum
            ? $"{Durations.Format(result)} is out of the range {Durations.Format(minimum)} to {Durations.Format(maximum)}."
            : null;
    }
}
