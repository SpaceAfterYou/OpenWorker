using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Extensions;

namespace OpenWorker.Hotspot.Modules.Chat.Responses;

public readonly record struct ChatMemoryStatisticsValue(int Count, int Size);

[HotspotMessage(Group, Command)]
public readonly struct ChatMemoryResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Chat;
    private const ChatOpcode Command = ChatOpcode.Memory;

    public float AllocatedMemory { get; init; }
    public float ResourceManagerMemory { get; init; }
    
    public ChatMemoryStatisticsValue Monster { get; init; }
    public ChatMemoryStatisticsValue Npc { get; init; }
    public ChatMemoryStatisticsValue Akashic { get; init; }
    public ChatMemoryStatisticsValue Projectile { get; init; }
    public ChatMemoryStatisticsValue Trap { get; init; }
    public ChatMemoryStatisticsValue Chain { get; init; }
    public ChatMemoryStatisticsValue Interaction { get; init; }
    public ChatMemoryStatisticsValue Vaccum { get; init; }
    public ChatMemoryStatisticsValue Maze { get; init; }
    public ChatMemoryStatisticsValue MyRoom { get; init; }
    public ChatMemoryStatisticsValue Social { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(AllocatedMemory);
        writer.Write(ResourceManagerMemory);
        
        writer.Write(Monster);
        writer.Write(Npc);
        writer.Write(Akashic);
        writer.Write(Projectile);
        writer.Write(Trap);
        writer.Write(Chain);
        writer.Write(Interaction);
        writer.Write(Vaccum);
        writer.Write(Maze);
        writer.Write(MyRoom);
        writer.Write(Social);
    }
}