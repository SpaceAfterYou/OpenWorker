using System.Globalization;

namespace OpenWorker.LoadTest.Cli;

/// <summary>
/// Reads and writes the lengths of time of the command line: a number with a unit (<c>500ms</c>, <c>30s</c>,
/// <c>5m</c>, <c>2h</c>), several of them in a row (<c>1m30s</c>), or a bare number of seconds.
/// </summary>
internal static class Durations
{
    public static bool TryParse(string text, out TimeSpan value)
    {
        value = default;

        var rest = text.AsSpan().Trim();

        if (rest.IsEmpty)
        {
            return false;
        }

        double seconds = 0;
        var any = false;

        while (!rest.IsEmpty)
        {
            var digits = 0;

            while (digits < rest.Length && (char.IsAsciiDigit(rest[digits]) || rest[digits] == '.'))
            {
                digits++;
            }

            if (digits == 0 || !double.TryParse(rest[..digits], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            rest = rest[digits..];

            var unit = 0;

            while (unit < rest.Length && char.IsAsciiLetter(rest[unit]))
            {
                unit++;
            }

            var factor = rest[..unit] switch
            {
                "" => 1d,
                "ms" => 0.001,
                "s" => 1d,
                "m" => 60d,
                "h" => 3600d,
                _ => -1d
            };

            // A bare number is the whole text: "30" is seconds, and "1m30" is a mistake.
            if (factor < 0 || (unit == 0 && any))
            {
                return false;
            }

            seconds += number * factor;
            any = true;
            rest = rest[unit..];
        }

        if (seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return false;
        }

        value = TimeSpan.FromSeconds(seconds);

        return true;
    }

    /// <summary>
    /// Writes a length of time the way it can be read back: whole hours, minutes and seconds, and milliseconds
    /// only when there are some.
    /// </summary>
    public static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            return "-" + Format(-value);
        }

        if (value == TimeSpan.Zero)
        {
            return "0s";
        }

        var text = new System.Text.StringBuilder();

        var hours = (long)value.TotalHours;

        if (hours > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"{hours}h");
        }

        if (value.Minutes > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"{value.Minutes}m");
        }

        if (value.Seconds > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"{value.Seconds}s");
        }

        if (value.Milliseconds > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"{value.Milliseconds}ms");
        }

        return text.ToString();
    }
}
