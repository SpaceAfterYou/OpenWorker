using Microsoft.EntityFrameworkCore;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Friends.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// The lists of friends and the people a person turned away, as the database keeps them.
/// </summary>
public sealed class FriendBook(IDbContextFactory<PersistenceContext> factory) : IPersonRelations
{
    public Task<bool> IsFriend(int person, int other) => factory.IsFriendAsync(person, other);

    public Task<bool> IsBlocked(int person, int other) => factory.IsBlockedAsync(person, other);

    ValueTask<bool> IPersonRelations.IsFriendAsync(int person, int other) => new(IsFriend(person, other));

    ValueTask<bool> IPersonRelations.IsBlockedAsync(int person, int other) => new(IsBlocked(person, other));

    /// <summary>
    /// A person as the client of another person lists them. The one who is online shows where, the others only how they were
    /// when they left.
    /// </summary>
    public static FriendValue ToValue(FriendEntry entry, PresenceRecord? online)
    {
        return new FriendValue
        {
            Name = entry.Name,
            Friend = entry.Person,
            Level = online?.Level ?? PersonModuleDefines.DefaultLevel,
            Class = entry.Hero,
            State = (FriendState)(byte)entry.Kind,
            CommunityState = (CommunityState)entry.CommunityState,
            Note = entry.Memo,
            Channel = (byte)(online?.Channel ?? 0),
            World = online?.Location ?? 0,
            FriendPoint = (ulong)Math.Max(0, entry.Point),
            Login = online is not null,
            LogOut = ToUnix(entry.LastLogout),
            Remain = 0
        };
    }

    public static ulong ToUnix(DateTime? value)
    {
        return value is { } time ? (ulong)new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)).ToUnixTimeSeconds() : 0;
    }
}
