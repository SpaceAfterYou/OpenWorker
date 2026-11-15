using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Lua;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The members of a player's party who play the same instance of an open maze of the world, the player among them.
/// </summary>
public sealed partial class WorldMazeParties(World world, IPartyRoster parties, CommunityNetwork network, ILogger<WorldMazeParties> logger)
{
    public async ValueTask<IReadOnlyList<Entity>> InMazeAsync(Entity player, LuaMaze maze)
    {
        if (!world.IsAlive(player) || !world.TryGet(player, out ActorComponent actor))
        {
            return [];
        }

        IReadOnlyList<int> persons;

        try
        {
            persons = await parties.MembersAsync(actor.Identifier);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogPartyUnknown(e, actor.Identifier);

            return [];
        }

        var found = new List<Entity>(persons.Count);

        foreach (var person in persons)
        {
            if (network.TryGet(person, out var member) && world.TryGet(member, out LuaMaze? playing) && ReferenceEquals(playing, maze))
            {
                found.Add(member);
            }
        }

        return found;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The party of person {Person} could not be read, its kill counts for it alone")]
    private partial void LogPartyUnknown(Exception exception, int person);
}
