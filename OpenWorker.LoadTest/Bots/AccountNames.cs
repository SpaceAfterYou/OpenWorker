using System.Globalization;

namespace OpenWorker.LoadTest.Bots;

/// <summary>
/// The names of the accounts the bots sign in with, and of their characters. The seed command and the bots both
/// make the names here, so what one creates the other finds.
/// </summary>
internal static class AccountNames
{
    /// <summary>
    /// The length of the username column of the database.
    /// </summary>
    public const int MaxLength = 12;

    private const int MinDigits = 5;

    public static string Format(string prefix, int number)
    {
        return prefix + number.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Whether the prefix and the numbers from <paramref name="first" /> make names the database can hold.
    /// </summary>
    public static bool Fits(string prefix, int first, int count)
    {
        var last = (long)first + Math.Max(count, 1) - 1;
        var digits = Math.Max(MinDigits, last.ToString(CultureInfo.InvariantCulture).Length);

        return prefix.Length + digits <= MaxLength;
    }

    /// <summary>
    /// A hardware address for the login. A real client sends its own; the server keeps it and never checks it, and
    /// the bots make one up from the number of the account.
    /// </summary>
    public static string MacAddress(int number)
    {
        return string.Create(CultureInfo.InvariantCulture, $"02-4C-54-{(number >> 16) & 0xFF:X2}-{(number >> 8) & 0xFF:X2}-{number & 0xFF:X2}");
    }
}
