using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a monster drops: the chance of each kind out of ten thousand and the groups the items come from.
/// </summary>
[BinaryResourceTable("tb_Drop", Optional = true)]
public readonly struct DropRow(BinaryReader reader)
{
    public const int GroupCount = 21;
    public const int CharacterGroupCount = 10;

    public int Id { get; } = reader.ReadInt32();
    public byte RateClear { get; } = reader.ReadByte();
    public ushort GoldChance { get; } = reader.ReadUInt16();
    public ushort GoldMin { get; } = reader.ReadUInt16();
    public ushort GoldMax { get; } = reader.ReadUInt16();
    public ushort GoldBonusChance { get; } = reader.ReadUInt16();
    public byte GoldBonusMultiple { get; } = reader.ReadByte();
    public ushort SoulChance { get; } = reader.ReadUInt16();
    public ushort SoulMin { get; } = reader.ReadUInt16();
    public ushort SoulMax { get; } = reader.ReadUInt16();
    public ushort EtherChance { get; } = reader.ReadUInt16();
    public ushort EtherMin { get; } = reader.ReadUInt16();
    public ushort EtherMax { get; } = reader.ReadUInt16();
    public ushort EtherUnit { get; } = reader.ReadUInt16();
    public ushort EtherBonusChance { get; } = reader.ReadUInt16();
    public ushort EtherBonusMultiple { get; } = reader.ReadUInt16();
    public ushort BattlePointChance { get; } = reader.ReadUInt16();
    // The table has the largest of the battle points first; some rows have the two the other way round.
    public ushort BattlePointMax { get; } = reader.ReadUInt16();
    public ushort BattlePointMin { get; } = reader.ReadUInt16();
    public ushort[] GroupChance { get; } = Enumerable.Range(0, GroupCount).Select(_ => reader.ReadUInt16()).ToArray();
    public int[] GroupId { get; } = Enumerable.Range(0, GroupCount).Select(_ => reader.ReadInt32()).ToArray();
    public ushort[] CharacterGroupChance { get; } = Enumerable.Range(0, CharacterGroupCount).Select(_ => reader.ReadUInt16()).ToArray();
    public int[] CharacterGroupId { get; } = Enumerable.Range(0, CharacterGroupCount).Select(_ => reader.ReadInt32()).ToArray();

    // The quest items come with the quest, they are read to reach the end of the row.
    private readonly int[] _quest = Enumerable.Range(0, 2).Select(_ => reader.ReadInt32()).ToArray();
    private readonly ushort[] _questChance = Enumerable.Range(0, 2).Select(_ => reader.ReadUInt16()).ToArray();
    private readonly byte[] _questType = Enumerable.Range(0, 2).Select(_ => reader.ReadByte()).ToArray();
    private readonly int[] _questItem = Enumerable.Range(0, 2).Select(_ => reader.ReadInt32()).ToArray();
    private readonly ushort[] _questMin = Enumerable.Range(0, 2).Select(_ => reader.ReadUInt16()).ToArray();
    private readonly ushort[] _questMax = Enumerable.Range(0, 2).Select(_ => reader.ReadUInt16()).ToArray();
}
