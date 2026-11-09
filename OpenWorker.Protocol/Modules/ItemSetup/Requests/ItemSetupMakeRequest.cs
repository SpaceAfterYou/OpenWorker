using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

/// <summary>
/// Makes items of a recipe of <c>tb_Make</c> at an NPC: the NPC, the recipe and how many items. The client sends two more
/// bytes after the count, which the server does not read.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemSetupMakeRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Make;

    public int Npc { get; init; }
    public int MakeIndex { get; init; }
    public byte MakeCount { get; init; }
    public short Unknown { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}