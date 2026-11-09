using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Domain.Components;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Tells the friends of a person when the person comes or goes, or says something new about themselves.
/// </summary>
public sealed class FriendNotifier(World world, CommunityNetwork network, IDbContextFactory<PersistenceContext> factory) : ICommunityListener
{
    public ValueTask OnEnteredAsync(Entity player, PresenceRecord person)
    {
        return TellAsync(player, person, online: true);
    }

    public async ValueTask OnLeftAsync(Entity player, PresenceRecord person)
    {
        await factory.SetLastLogoutAsync(person.Person, DateTime.UtcNow);
        await TellAsync(player, person, online: false);
    }

    /// <summary>
    /// A person who is connected right now changed what they say about themselves.
    /// </summary>
    public async ValueTask ChangedAsync(Entity player)
    {
        var person = await network.FindAsync(world.Get<ActorComponent>(player).Identifier);

        if (person is not null && world.IsAlive(player))
        {
            await TellAsync(player, person, online: true);
        }
    }

    private async ValueTask TellAsync(Entity player, PresenceRecord person, bool online)
    {
        var points = await factory.FriendPointsAsync(person.Person);

        if (points.Count == 0)
        {
            return;
        }

        var community = world.Has<CommunityComponent>(player)
            ? world.Get<CommunityComponent>(player)
            : new CommunityComponent { State = CommunityState.LookingForParty, Comment = string.Empty, Memo = string.Empty };

        var friends = await network.FindManyAsync(points.Keys.ToArray());
        var now = DateTime.UtcNow;

        foreach (var friend in friends.OfType<PresenceRecord>())
        {
            // The entry of the person in the list of the friend.
            var entry = new FriendEntry(
                person.Person,
                person.Name,
                person.Hero,
                FriendKind.Friend,
                points[friend.Person],
                (byte)community.State,
                community.Memo,
                online ? null : now,
                now,
                person.Level);

            await network.SendAsync(friend.Person, new FriendUpdateInfoResponse { Value = FriendBook.ToValue(entry, online ? person : null) });
        }
    }
}
