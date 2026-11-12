using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct FriendBlockValue(
    int Person,
    [property: WireString(WireStringFormat.Utf16Unicode)] string Name,
    byte Level);
