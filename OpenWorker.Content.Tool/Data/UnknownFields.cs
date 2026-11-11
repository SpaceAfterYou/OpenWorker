using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenWorker.Content.Tool.Models;
using OpenWorker.Res.Attributes;

namespace OpenWorker.Content.Tool.Data;

/// <summary>
/// Columns still named <c>FieldN</c> on a generated row: their meaning is not settled, but the tool shows them under the
/// name of the client table they come from.
/// </summary>
internal static partial class UnknownFields
{
    public static UnknownFieldTable Of<T>(in T row) where T : struct
    {
        object boxed = row;
        var fields = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => FieldName().IsMatch(property.Name))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => new ItemPropertyRow
            {
                Name = property.Name,
                Value = Format(property.GetValue(boxed))
            })
            .ToArray();

        return new UnknownFieldTable
        {
            TableName = TableNameOf(typeof(T)),
            Fields = fields
        };
    }

    private static string TableNameOf(Type type)
    {
        var attribute = type.GetCustomAttribute<BinaryResourceTableAttribute>();
        return attribute?.Table ?? type.Name;
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
