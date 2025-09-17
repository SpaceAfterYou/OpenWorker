using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupMakeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Make;

    public int Npc { get; } = reader.ReadInt32();
    public int MakeIndex { get; } = reader.ReadInt32();
    public short Unknown { get; } = reader.ReadInt16();
    public byte MakeCount { get; } = reader.ReadByte();
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupUpgradeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Upgrade;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupExchangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Exchange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupDisassembleRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Disassemble;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupSocketEquipRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketEquip;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupSocketActiveRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketActive;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupSocketDetachRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketDetach;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRepairRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Repair;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRepairNpcRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RepairNpc;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRepairAllRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RepairAll;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupEnduranceRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Endurance;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupEvolutionRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Evolution;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicMakeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicMake;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicDisassembleRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicDisassemble;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupUpgradeLimitRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.UpgradeLimit;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicComposeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicCompose;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupDisassembleExRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.DisassembleEx;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupBroachEquipRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachEquip;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupBroachActiveRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachActive;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRestoreRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Restore;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupBroachComposeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachCompose;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupUnsealRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Unseal;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicMakeExRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicMakeEx;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupUseEffectRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.UseEffect;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRenovateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Renovate;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupBroachRemoveRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachRemove;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRefineRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Refine;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupSocketExchangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketExchange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupSocketUpgradeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketUpgrade;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupSocketExtractRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketExtract;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicComposeExRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicComposeEx;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicGetInfoAddRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicGetInfoAdd;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupAkashicGetInfoLoadRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicGetInfoLoad;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupDyeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Dye;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupTitleChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.TitleChange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupRenovateCompleteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RenovateComplete;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ItemSetupBroachRemoveExRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachRemoveEx;

    public MessageOpcode Opcode => new(Group, Command);
}