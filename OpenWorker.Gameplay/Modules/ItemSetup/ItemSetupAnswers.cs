using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// How the work on items answers the client: the answer of the request, or its error. Every request of the group is
/// answered, refused or not, since the client keeps its dialog waiting for it.
/// </summary>
public sealed class ItemSetupAnswers(World world)
{
    /// <summary>A slot that cannot hold what the request says.</summary>
    public const ushort WrongSlot = 52012;

    public void Refuse(Entity player, ItemSetupOpcode command, ushort code)
    {
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.ItemSetup, command), code));
    }

    public void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }
}
