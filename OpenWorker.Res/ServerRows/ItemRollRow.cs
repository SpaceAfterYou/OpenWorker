using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What the server's own <c>tb_Item</c> says of an item that the client's does not: the most its attack and its defence can be
/// (the client's table has the least) and the table of the options its upgrade levels give. The original rolls the attack of an
/// item between the least and the most when the item is made.
/// </summary>
[BinaryResourceTable("tb_Item", Server = true, Optional = true)]
public readonly struct ItemRollRow
{
    public ItemRollRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();

        // The kind of item, its rank, level, prices, stack, binding and model.
        reader.ReadBytes(sizeof(int) + sizeof(byte) + sizeof(ushort) + 4 * sizeof(uint) + sizeof(ushort) + sizeof(byte) + sizeof(uint));

        // Its name.
        reader.ReadBytes(reader.ReadUInt16() * sizeof(char));

        // The level and class it asks for, whether it can be sold, its sub type, its costume set.
        reader.ReadBytes(sizeof(ushort) + 3 * sizeof(byte) + sizeof(byte) + sizeof(uint));

        // The slots it disables.
        reader.ReadBytes(reader.ReadUInt16() * sizeof(char));

        // Its endurance and its use value.
        reader.ReadBytes(2 * sizeof(byte));

        AttackLeast = reader.ReadInt32();
        Attack = reader.ReadInt32();

        // The magical attack.
        reader.ReadInt32();

        DefenseLeast = reader.ReadInt32();
        Defense = reader.ReadInt32();

        // The magical defence, then five stat options each as a class, a kind and a value.
        reader.ReadInt32();
        reader.ReadBytes(5 * sizeof(byte) + 5 * sizeof(uint) + 5 * sizeof(int));

        // The extra option, the sockets, the set and the table of the upgrade.
        reader.ReadBytes(3 * sizeof(uint) + sizeof(ushort));
        ReinforceOption = reader.ReadInt32();

        // The title, the evolution, the disassembly, the furniture, the cooldown, the effect, cash, period, seals, the similar group
        // and the item it is repackaged to.
        reader.ReadBytes(sizeof(uint) + sizeof(uint) + sizeof(ushort) + sizeof(uint) + sizeof(ushort) + sizeof(uint) + sizeof(byte)
            + sizeof(ushort) + sizeof(uint) + 2 * sizeof(byte) + sizeof(uint) + 2 * sizeof(byte) + sizeof(uint) + sizeof(uint));
    }

    public ItemRollRow(int id, int attackLeast, int attack, int defenseLeast, int defense, int reinforceOption)
    {
        Id = id;
        AttackLeast = attackLeast;
        Attack = attack;
        DefenseLeast = defenseLeast;
        Defense = defense;
        ReinforceOption = reinforceOption;
    }

    public int Id { get; }

    public int AttackLeast { get; }

    public int Attack { get; }

    public int DefenseLeast { get; }

    public int Defense { get; }

    /// <summary>
    /// The row of <c>tb_Reinforce_Option</c> that names the options the upgrade levels of the item give, zero for none.
    /// </summary>
    public int ReinforceOption { get; }
}
