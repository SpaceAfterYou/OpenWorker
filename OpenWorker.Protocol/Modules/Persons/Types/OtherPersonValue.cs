using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Types;

/// <summary>
/// What is shown about another player.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct OtherPersonValue
{
    public short Level { get; init; }
    public CommunityState State { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Comment { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<ST_STAT> BaseStats { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<ST_STAT> Stats { get; init; }

    [WireArray(WireLength.Signed32)]
    public IReadOnlyCollection<StorageValue> Equipment { get; init; }

    public byte EchelonLevel { get; init; }
    public int EchelonExperience { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Memo { get; init; }

    public int EquipMemorySlot { get; init; }
    public byte Class { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }

    public TitleValue Ability { get; init; }
    public TitleValue Represent { get; init; }

    [WireArray(WireLength.Byte)]
    public ItemSocketValue[] Sockets { get; init; }

    [WireArray(WireLength.Byte)]
    public ItemBroachValue[] Broaches { get; init; }
}
