namespace OpenWorker.Channel.Community;

/// <summary>
/// What a server does when the auto matching has made a party for a maze: the district sends the party into it.
/// </summary>
public interface IPartyMatchListener
{
    /// <param name="party">The party the group became.</param>
    /// <param name="maze">The maze the group was made for.</param>
    /// <param name="persons">Everybody in it, the master first.</param>
    ValueTask OnMatchedAsync(int party, short maze, IReadOnlyList<int> persons);
}
