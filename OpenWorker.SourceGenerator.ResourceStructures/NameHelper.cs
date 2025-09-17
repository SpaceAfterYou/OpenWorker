using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenWorker.SourceGenerator.ResourceStructures;

internal static partial class NameHelper
{
    internal static string GetClassName(string name)
    {
        var info = CultureInfo.GetCultureInfo("en-US").TextInfo;

        if (name.StartsWith("tb_", StringComparison.CurrentCultureIgnoreCase))
        {
            name = name[3..];
        }

        return info
            .ToTitleCase(name.SplitCamelCase(" ").Replace("_", " "))
            .Replace(" ", string.Empty);
    }

    private static string SplitCamelCase(this string input, string delimiter)
    {
        return input.Any(char.IsUpper) ? string.Join(delimiter, IsUpperLetter().Split(input)) : input;
    }

    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex IsUpperLetter();
}