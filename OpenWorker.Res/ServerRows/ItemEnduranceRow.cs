using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// How fast the gear of a hero wears, from <c>tb_Item_Endurance</c>, a table only the server has: one row per hero. A chance
/// out of ten thousand and what is taken away, for the weapon when its blow lands and when its carrier falls, and for the
/// armour when a blow lands on its carrier and when its carrier falls.
/// </summary>
[BinaryResourceTable("tb_Item_Endurance", Optional = true)]
public readonly struct ItemEnduranceRow
{
    public ItemEnduranceRow(BinaryReader reader)
    {
        Hero = reader.ReadInt32();
        WeaponAttackChance = reader.ReadUInt16();
        reader.ReadUInt16();
        WeaponAttackLoss = reader.ReadSingle();
        reader.ReadSingle();
        WeaponDeathChance = reader.ReadUInt16();
        reader.ReadUInt16();
        WeaponDeathLoss = reader.ReadSingle();
        reader.ReadSingle();
        GearDefenceChance = reader.ReadUInt16();
        GearDefenceLoss = reader.ReadSingle();
        GearDeathChance = reader.ReadUInt16();
        GearDeathLoss = reader.ReadSingle();
    }

    public int Hero { get; }

    public ushort WeaponAttackChance { get; }

    public float WeaponAttackLoss { get; }

    public ushort WeaponDeathChance { get; }

    public float WeaponDeathLoss { get; }

    public ushort GearDefenceChance { get; }

    public float GearDefenceLoss { get; }

    public ushort GearDeathChance { get; }

    public float GearDeathLoss { get; }
}
