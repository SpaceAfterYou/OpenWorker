using Arch.Core;
using CommunityToolkit.HighPerformance;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Login.Components;

namespace OpenWorker.Hotspot.Modules.Login.Responses;

[HotspotMessage(Group, Command)]
public readonly struct LoginOptionLoadResponse(Arch.Core.World world, Entity player, ServerContent contents) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Login;
    private const LoginOpcode Command = LoginOpcode.OptionLoad;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(world.Get<PersonOptionComponent>(player).Collection);
        writer.Write(contents);
    }
}