namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// Event boxes of one kind on the selected map, for the sidebar tree.
/// </summary>
public sealed class BatchBoxGroup
{
    public required string Kind { get; init; }

    public required string Title { get; init; }

    public required IReadOnlyList<BatchBoxDraw> Boxes { get; init; }
}
