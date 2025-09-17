using Arch.Core;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Hotspot.Commands.Persons;

[StuffCommand("exp")]
public sealed class PersonExpCommand(World world) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "exp (value) [additional] [bonus]";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        if (int.TryParse(tokens.ElementAtOrDefault(0), out var value) is false)
        {
            SendTutorial(player, nameof(value));
            
            return ValueTask.FromResult(false);
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(1), out var additional);
        _ = int.TryParse(tokens.ElementAtOrDefault(2), out var bonus);

        var session = World.Get<ServerSessionComponent>(player);
        
        session.Send(new PersonExpResponse(value, additional, bonus));
        
        return ValueTask.FromResult(true);
    }
}