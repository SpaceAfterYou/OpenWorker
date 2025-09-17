using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

internal static class BinaryWriterExtension
{
    internal static void Write(this BinaryWriter writer, SoulMetryValue value)
    {
        writer.Write(value.Identifier);
        writer.Write(value.CompletionBit);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SoulMetryListResponse(IReadOnlyList<SoulMetryValue> list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.List;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write((short)list.Count);

        foreach (var value in list)
        {
            writer.Write(value);
        }
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SoulMetryCompleteListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.CompleteList;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SoulMetryAddResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Add;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SoulMetryUpdateResponse(int identifier, short value) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Update;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(new SoulMetryValue(identifier, value));
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SoulMetryCompleteResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Complete;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SoulMetryResetResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Reset;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
    }
}