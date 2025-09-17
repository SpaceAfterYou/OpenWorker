using Arch.Core;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Login.Responses;

namespace OpenWorker.Hotspot.Commands;

[StuffCommand("option")]
public sealed class OptionCommand(World world, ServerContent contents) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "option (content index) (flag)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);
        
        var response = new LoginOptionLoadResponse(World, player, contents);
        
        if (int.TryParse(tokens.ElementAtOrDefault(0), out var index) is false || index < 0 || index >= contents.Count)
        {
            SendTutorial(player, nameof(index));
            return ValueTask.FromResult(false);
        }
        
        if (bool.TryParse(tokens.ElementAtOrDefault(1), out var flag) is false)
        {
            SendTutorial(player, nameof(flag));
            return ValueTask.FromResult(false);
        }

        contents[index] = flag;
        
        session.Send(response);
        return ValueTask.FromResult(true);
    }
}