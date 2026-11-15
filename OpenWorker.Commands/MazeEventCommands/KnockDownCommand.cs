using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Lua;

namespace OpenWorker.Commands.MazeEventCommands;

/// <summary>
/// The nearest monster throws the player with a blow of one of its own skills that hurts it a little: <c>/knock-down</c>. The
/// player flies, then lies on the ground until it gets up.
/// </summary>
[CommandTrigger("knock", "down")]
[CommandDescription("Make the nearest monster throw you with a blow of its own skill")]
internal sealed partial class KnockDownCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!World.Has<LuaMaze>(player) || tokens.Length != 0)
        {
            return ValueTask.FromResult(false);
        }

        var thrown = World.Get<LuaMaze>(player).KnockDown(player);

        if (thrown is { Monster: not 0, Skill: 0 })
        {
            World.Get<ServerSessionComponent>(player).Send(new ChatNormalResponse
            {
                Actor = World.Get<ActorComponent>(player),
                Appearance = ChatMessageAppearance.System,
                Message = string.Create(CultureInfo.InvariantCulture, $"Monster {thrown.Monster} has no blow that throws a player.")
            });
        }

        return ValueTask.FromResult(thrown.Hit);
    }
}
