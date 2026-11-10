namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One named field of an item for the detail table.
/// </summary>
public sealed class ItemPropertyRow
{
    public required string Name { get; init; }

    public required string Value { get; init; }
}
