using System.Text;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;

namespace OpenWorker.Hotspot.Commands;

public abstract class AStuffCommand(World world)
{
    protected World World { get; } = world;
    
    public abstract ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens);

    protected abstract string GetTutorialMessage();
    
    private string GetTutorialMessage(string? argument, string? message)
    {
        var builder = new StringBuilder();

        if (message is not null)
        {
            builder.Append($"({argument}). ");
        }
        
        if (argument is not null)
        {
            builder.Append($"Bad argument: {argument}. ");
        }

        builder.Append("Usage /");
        builder.Append(GetTutorialMessage());
        
        return builder.ToString();
    }

    protected void SendTutorial(Entity entity, string? argument = null, string? message = null)
    {
        var session = World.Get<ServerSessionComponent>(entity);
        var actor = World.Get<ActorComponent>(entity);
        
        session.Send(new ChatNormalResponse(actor, ChatMessageAppearance.System, GetTutorialMessage(argument, message)));
    }
}