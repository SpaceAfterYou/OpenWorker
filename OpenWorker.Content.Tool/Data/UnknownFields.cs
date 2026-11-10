using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenWorker.Content.Tool.Models;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Columns still named <c>FieldN</c> on a generated row: their meaning is not settled, but the tool shows them.
/// </summary>
internal static partial class UnknownFields
{
    public static IReadOnlyList<ItemPropertyRow> Of<T>(in T row) where T : struct
    {
        object boxed = row;
        return typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => FieldName().IsMatch(property.Name))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => new ItemPropertyRow
            {
                Name = property.Name,
                Value = Format(property.GetValue(boxed))
            })
            .ToArray();
    }

    private static string Format(object? value) =>
        value switch
        {
            null => string.Empty,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    [GeneratedRegex("^Field\\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldName();
}
