using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Hotspot.Dtos;

public readonly struct CharacterInfoGatePayload
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
    public string AccountId { get; init; }
    public bool NetCafe { get; init; }
    public bool ClassScene { get; init; }
    public WorldType WorldType { get; init; }
    public bool UsePvpDistrict { get; init; }
}
