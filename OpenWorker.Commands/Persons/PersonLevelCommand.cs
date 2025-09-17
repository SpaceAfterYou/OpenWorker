using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Hotspot.Commands.Persons;

[StuffCommand("level")]
public sealed class PersonLevelCommand(World world) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "level (level)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var actor = World.Get<ActorComponent>(player);

        if (byte.TryParse(tokens.ElementAtOrDefault(0), out var level) is false)
        {
            SendTutorial(player, nameof(level));
            
            return ValueTask.FromResult(false);
        }

        session.Send(new PersonLevelResponse(actor, level));
        return ValueTask.FromResult(true);
    }
}