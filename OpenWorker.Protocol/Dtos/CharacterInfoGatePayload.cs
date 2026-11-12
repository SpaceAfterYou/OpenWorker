using OpenWorker.Hotspot.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Dtos;

/// <summary>
/// What the gate adds to the info of a character: the money and points of the account and how the world is played.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct CharacterInfoGatePayload
{
    public int PlaceholderInt { get; init; }
    public long Gold { get; init; }
    public byte CommonStep { get; init; }
    public byte ConsumeStep { get; init; }
    public byte CostumeStep { get; init; }
    public byte CardStep { get; init; }
    public uint UserDb { get; init; }
    public uint SyncUser { get; init; }
    public long BattlePoint { get; init; }
    public long Ether { get; init; }
    public long FriendPoint { get; init; }
    [WireString(WireStringFormat.Utf8Ascii)]
    public string AccountId { get; init; }
    public bool NetCafe { get; init; }
    public bool ClassScene { get; init; }
    public WorldType WorldType { get; init; }
    public bool UsePvpDistrict { get; init; }
}
