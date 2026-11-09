using Arch.Core;
using OpenWorker.Gameplay.Combat;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// How the work on items answers the client: the answer of the request, or its error. Every request of the group is
/// answered, refused or not, since the client keeps its dialog waiting for it. After an answer the player is told the stats
/// the work changed (an upgrade, a gem, a broach, a repair of what it wears), when it changed any.
/// </summary>
public sealed class ItemSetupAnswers(World world, PlayerStatsService? stats = null)
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

        stats?.Send(player);
    }
}
