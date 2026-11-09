using Arch.Core;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Server.Common;

/// <summary>
/// The access level a player's commands are judged by: the level of its account, or a game master's for everybody on a test
/// stand (<see cref="CommandOptions.OpenToEveryone"/>).
/// </summary>
public sealed class CommandAccess(World world, IOptions<CommandOptions> options)
{
    public AccessLevel LevelOf(Entity player)
    {
        var level = world.IsAlive(player) && world.TryGet<AccessComponent>(player, out var access) ? access.Level : AccessLevel.None;

        return options.Value.OpenToEveryone && level < AccessLevel.GameMaster ? AccessLevel.GameMaster : level;
    }
}
