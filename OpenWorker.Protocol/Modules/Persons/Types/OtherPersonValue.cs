using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// What is shown about another player.
/// </summary>
public readonly struct OtherPersonValue
{
    public short Level { get; init; }
    public CommunityState State { get; init; }
    public string Comment { get; init; }
    public IReadOnlyCollection<ST_STAT> BaseStats { get; init; }
    public IReadOnlyCollection<ST_STAT> Stats { get; init; }
    public IReadOnlyCollection<StorageValue> Equipment { get; init; }
    public byte EchelonLevel { get; init; }
    public int EchelonExperience { get; init; }
    public string Memo { get; init; }
    public int EquipMemorySlot { get; init; }
    public byte Class { get; init; }
    public string Name { get; init; }
    public TitleValue Ability { get; init; }
    public TitleValue Represent { get; init; }
    public ItemSocketValue[] Sockets { get; init; }
    public ItemBroachValue[] Broaches { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Level);
        writer.Write((byte)State);
        writer.WriteCommunityComment(Comment);
        writer.Write(BaseStats);
        writer.Write(Stats);
        writer.Write(Equipment.Count);

        foreach (var item in Equipment)
        {
            writer.Write(item);
        }

        writer.Write(EchelonLevel);
        writer.Write(EchelonExperience);
        writer.WriteCommunityMemo(Memo);
        writer.Write(EquipMemorySlot);
        writer.Write(Class);
        writer.WritePersonName(Name);
        writer.Write(Ability.Value);
        writer.Write(Represent.Value);
        writer.Write(Sockets);
        writer.Write(Broaches);
    }
}
