using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Lua;

namespace OpenWorker.Commands.Persons;

/// <summary>
/// Kills the player: <c>/die</c>. In a maze the player falls as a blow that took the last of its life would bring it down, and
/// may get up; elsewhere the client is only told that it fell.
/// </summary>
[CommandTrigger("die")]
[CommandDescription("Kill player")]
internal sealed partial class PersonDieCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        _ = int.TryParse(tokens.ElementAtOrDefault(0), out var pvpKillCount);

        if (World.Has<LuaMaze>(player))
        {
            World.Get<LuaMaze>(player).Creatures.Fell(player);

            return ValueTask.FromResult(true);
        }

        PrivateExecute(player, pvpKillCount);

        return ValueTask.FromResult(true);
    }

    private void PrivateExecute(Entity player, int pvpKillCount)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var actor = World.Get<ActorComponent>(player);

        session.Send(new PersonDieResponse { Victim = actor, Attacker = actor, PvpKillCount = pvpKillCount });
    }
}
