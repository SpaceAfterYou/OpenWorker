using OpenWorker.Hotspot.Modules.Items.Requests;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

public readonly record struct ItemMoveInfoValue
{
    public required MoveItemValue Src { get; init; }
    public required MoveItemValue Dest { get; init; }

    /// <summary>
    /// TODO: What is it
    /// </summary>
    public byte SrcBind { get; init; }
    
    /// <summary>
    /// TODO: What is it
    /// </summary>
    public byte DestBind { get; init; }
}