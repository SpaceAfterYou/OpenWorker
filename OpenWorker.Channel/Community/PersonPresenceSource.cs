using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Says what the others should know about a person who is connected to this server.
/// </summary>
public interface IPresenceSource
{
    PresenceRecord Describe(Entity player, Guid instance);
}

internal sealed partial class PersonPresenceSource(
    World world,
    ServiceChannels channels,
    BatchManager batches,
    ILogger<PersonPresenceSource> logger
) :
    IPresenceSource
{
    public PresenceRecord Describe(Entity player, Guid instance)
    {
        var actor = world.Get<ActorComponent>(player);
        var info = world.Get<PersonInfoComponent>(player);
        var place = world.Get<WorldComponent>(player);

        var channel = channels.Count > 0 && world.Has<ChannelMemberComponent>(player)
            ? channels[world.Get<ChannelMemberComponent>(player).Index].Identifier
            : (short)0;

        var options = world.Has<PersonOptionComponent>(player) ? world.Get<PersonOptionComponent>(player).Collection : null;

        return new PresenceRecord(
            instance,
            actor.Identifier,
            info.Name,
            (byte)info.Hero,
            // Levels are not earned yet, the character is shown at the one it has everywhere else.
            PersonModuleDefines.DefaultLevel,
            place.Location,
            channel,
            IsMaze(place.Location),
            PersonOptions.Level(options, PersonOptions.Whisper),
            PersonOptions.Level(options, PersonOptions.OtherInfo));
    }

    private bool IsMaze(short location)
    {
        try
        {
            return batches.TryGetAndCache(location, out _, out var type) && type == BatchType.Maze;
        }

        catch (Exception e)
        {
            LogLocationKindUnknown(logger, e, location);

            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Could not tell the kind of location {Location}.")]
    private static partial void LogLocationKindUnknown(ILogger logger, Exception exception, short location);
}
