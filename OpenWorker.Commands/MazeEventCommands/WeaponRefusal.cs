using System.Globalization;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// The line a command answers with when it is asked for a defensive weapon, which it never brings in.
/// </summary>
internal static class WeaponRefusal
{
    public static void Tell(World world, Entity player, int monster)
    {
        world.Get<ServerSessionComponent>(player).Send(new ChatNormalResponse
        {
            Actor = world.Get<ActorComponent>(player),
            Appearance = ChatMessageAppearance.System,
            Message = string.Create(CultureInfo.InvariantCulture,
                $"Monster {monster} is a defensive weapon: the client has no model for it, and every client that is shown it fails.")
        });
    }
}
