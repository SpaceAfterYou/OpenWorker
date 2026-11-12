using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[WireMessage(WireMethods.Write)]
public readonly partial record struct ItemMoveInfoValue
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
