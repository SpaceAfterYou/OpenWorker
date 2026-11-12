using System.Text.Json;

namespace OpenWorker.SourceGenerator.ResourceStructures;

/// <summary>
/// One column of a client table, as <c>Structures.json</c> declares it: the type the file stores, then the name, then the
/// enum it is read as: <c>"Int16"</c>, <c>"Int32 Classify"</c>, <c>"Byte OptionType as OpenWorker.Res.DivergenceVariant"</c>.
/// A column without a name is <c>Id</c> when it is the first, <c>Field</c> and its place otherwise. What follows
/// <c>//</c> says what the column means and becomes the summary of the property:
/// <c>"Byte BindType // When the item is bound."</c>.
/// </summary>
public sealed record ColumnDefinition(string Type, string Name, string? As, string? Comment = null)
{
    private const string CommentMark = "//";

    private static readonly Dictionary<string, string> Keywords = new(StringComparer.Ordinal)
    {
        ["Boolean"] = "bool",
        ["Byte"] = "byte",
        ["SByte"] = "sbyte",
        ["Int16"] = "short",
        ["UInt16"] = "ushort",
        ["Int32"] = "int",
        ["UInt32"] = "uint",
        ["Int64"] = "long",
        ["UInt64"] = "ulong",
        ["Single"] = "float",
        ["Double"] = "double",
        ["String"] = "string"
    };

    /// <summary>
    /// The C# type of the property: the enum when the column is read as one.
    /// </summary>
    public string PropertyType => As is null ? Keywords[Type] : As[(As.LastIndexOf('.') + 1)..];

    public static ColumnDefinition Parse(string text, int place)
    {
        var mark = text.IndexOf(CommentMark, StringComparison.Ordinal);
        var comment = mark < 0 ? null : text[(mark + CommentMark.Length)..].Trim();

        if (comment is { Length: 0 })
        {
            comment = null;
        }

        var parts = (mark < 0 ? text : text[..mark]).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is not (1 or 2 or 4) || !Keywords.ContainsKey(parts[0]) || parts.Length == 4 && parts[2] != "as")
        {
            throw new FormatException($"Column {place} \"{text}\" is not \"Type [Name [as Enum]]\".");
        }

        var name = parts.Length > 1 ? parts[1] : place == 0 ? "Id" : $"Field{place}";

        return new ColumnDefinition(parts[0], name, parts.Length == 4 ? parts[3] : null, comment);
    }
}

/// <summary>
/// A client table: its name in the files and its columns in the order a row stores them.
/// </summary>
public sealed record TableDefinition(string Table, IReadOnlyList<ColumnDefinition> Columns)
{
    /// <summary>
    /// The struct of a row: the name of the table without <c>tb_</c>, in Pascal case, and <c>Row</c>.
    /// </summary>
    public string RowName => NameHelper.GetClassName(Table) + "Row";

    /// <summary>
    /// The tables of <c>Structures.json</c>, an object of the table names, each with the list of its columns.
    /// </summary>
    public static IReadOnlyList<TableDefinition> Load(string path)
    {
        using var stream = File.OpenRead(path);

        var tables = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(stream)
            ?? throw new FormatException($"{path} holds no tables.");

        return tables
            .Select(table => new TableDefinition(table.Key, table.Value.Select(ColumnDefinition.Parse).ToArray()))
            .ToArray();
    }
}
