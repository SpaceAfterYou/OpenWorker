using System.Collections.Concurrent;
using OpenWorker.LoadTest.Cli;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Bots;

/// <summary>
/// What the bots of a run share: the options, the statistics, and the list of who is who. All the bots live in
/// one process, so a bot that receives the broadcast of another bot's move finds that bot here by its actor and
/// reads when it sent the move.
/// </summary>
internal sealed class RunContext
{
    private readonly ConcurrentDictionary<int, Bot> _actors = new();

    public required RunOptions Options { get; init; }

    public required RunStats Stats { get; init; }

    public Bot? Find(int actor)
    {
        return _actors.TryGetValue(actor, out var bot) ? bot : null;
    }

    public void Register(int actor, Bot bot)
    {
        _actors[actor] = bot;
    }

    public void Unregister(int actor, Bot bot)
    {
        _actors.TryRemove(new KeyValuePair<int, Bot>(actor, bot));
    }

    /// <summary>
    /// Whether the bot with this number goes to a maze. The ones that do are spread evenly over the numbers, so
    /// a run of any size has the share it asked for, to within one bot.
    /// </summary>
    public static bool GoesToMaze(int index, double share)
    {
        return Math.Floor((index + 1) * share) > Math.Floor(index * share);
    }
}
