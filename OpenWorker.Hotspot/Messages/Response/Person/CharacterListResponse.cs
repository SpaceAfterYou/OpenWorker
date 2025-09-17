using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Login.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person;

[HotspotMessage(Group, Command)]
public readonly struct CharacterListResponse(Entity entity) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.ListRes;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        var component = entity.Get<GatePersonComponent>();

        var list = component.SlotList
            .Where(x => x != Entity.Null)
            .ToArray();
        
        writer.Write((byte)list.Length);
        
        foreach (var person in list)
        {
            writer.Write(new PersonValue(entity, person));
        }
        
        writer.Write(component.LastIndex);
        
        writer.WriteProtectionState(entity);
    }
}

// https://youtu.be/ZZvJY7_gyfw?list=RDVfoCTeznCAk
// https://youtu.be/lyDAsYdNtt0?list=RDVfoCTeznCAk