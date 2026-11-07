using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Channel.Community;

/// <summary>
/// Who stands with a person in a party. A person alone gets an empty list.
/// </summary>
public interface IPartyRoster
{
    ValueTask<IReadOnlyList<int>> MembersAsync(int person);
}

/// <summary>
/// Everybody who belongs to a league.
/// </summary>
public interface ILeagueRoster
{
    ValueTask<IReadOnlyList<int>> MembersAsync(int league);
}

/// <summary>
/// Who has the other in their friend list or block list.
/// </summary>
public interface IPersonRelations
{
    ValueTask<bool> IsFriendAsync(int person, int other);

    ValueTask<bool> IsBlockedAsync(int person, int other);
}

/// <summary>
/// The use of a megaphone: the item has to be taken from the bag before the words go out.
/// </summary>
public interface IMegaphoneSource
{
    /// <summary>
    /// Returns zero when the item was taken, otherwise the code of the message the client shows.
    /// </summary>
    ValueTask<ushort> TakeAsync(Entity player, StorageGroup storage, short slot);
}

/// <summary>
/// Nobody is in a party until parties are kept somewhere.
/// </summary>
internal sealed class NoPartyRoster : IPartyRoster
{
    public ValueTask<IReadOnlyList<int>> MembersAsync(int person) => ValueTask.FromResult<IReadOnlyList<int>>([]);
}

/// <summary>
/// The bag does not keep a megaphone yet, so every use is free.
/// </summary>
internal sealed class FreeMegaphone : IMegaphoneSource
{
    public ValueTask<ushort> TakeAsync(Entity player, StorageGroup storage, short slot) => ValueTask.FromResult<ushort>(0);
}
