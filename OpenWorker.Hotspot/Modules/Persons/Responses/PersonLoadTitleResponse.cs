using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, Command)]
public readonly struct PersonLoadTitleResponse(IReadOnlyCollection<int> titleList, IReadOnlyCollection<int> openList, bool result) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.LoadTitle;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(result);

        Write(writer, titleList);
        Write(writer, openList);
    }

    private static void Write(BinaryWriter writer, IReadOnlyCollection<int> list)
    {
        writer.Write((byte)list.Count);

        foreach (var item in list)
        {
            writer.Write(item);
        }
    }
}