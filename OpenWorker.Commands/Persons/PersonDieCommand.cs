using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Hotspot.Commands.Persons;

[StuffCommand("die")]
public sealed class PersonDieCommand(World world) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "die [pvpKillCount]";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var actor = World.Get<ActorComponent>(player);

        _ = int.TryParse(tokens.ElementAtOrDefault(0), out var pvpKillCount);

        session.Send(new PersonDieResponse(actor, actor, pvpKillCount));
        return ValueTask.FromResult(true);
    }
}