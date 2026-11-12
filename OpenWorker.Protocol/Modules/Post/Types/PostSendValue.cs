using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Post.Types;

/// <summary>
/// A letter as the client sends it: the receiver, the title, the text, the gold and the five places of attachments.
/// </summary>
[WireMessage(WireMethods.Read)]
public readonly partial struct PostSendValue
{
    [WireString(WireStringFormat.Utf8Unicode)]
    public string TargetName { get; init; }

    [WireString(WireStringFormat.Utf8Unicode)]
    public string Title { get; init; }

    [WireString(WireStringFormat.Utf8Unicode)]
    public string Message { get; init; }

    public long Money { get; init; }

    [WireArray(Count = PostModuleDefines.ItemCount)]
    public PostSendItemValue[] Items { get; init; }
}
