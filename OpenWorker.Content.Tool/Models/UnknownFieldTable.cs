using System.Globalization;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// The still-unnamed <c>FieldN</c> columns of one client table, with the table name for the detail caption.
/// </summary>
public sealed class UnknownFieldTable
{
    public required string TableName { get; init; }

    public required IReadOnlyList<ItemPropertyRow> Fields { get; init; }

    public bool HasFields => Fields.Count > 0;

    public string Title =>
        string.Format(CultureInfo.InvariantCulture, "Unknown fields — {0}", TableName);
}
