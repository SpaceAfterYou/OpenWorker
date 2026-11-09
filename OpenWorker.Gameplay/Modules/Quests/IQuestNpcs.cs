using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// How far a person stands from a character of the map, as far as the server knows where the character stands.
/// </summary>
public enum NpcReach : byte
{
    /// <summary>
    /// The server does not know where the character stands here: a script placed it, or the map is not one the server
    /// reads its characters from.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The person stands close enough to talk to it.
    /// </summary>
    Near = 1,

    /// <summary>
    /// The character stands on this map, but too far from the person.
    /// </summary>
    Far = 2
}

/// <summary>
/// Where the characters a quest is taken from, handed in to or talked to stand. The client says it has talked to a
/// character; the server checks that the person is near it when it knows where the character is.
/// </summary>
public interface IQuestNpcs
{
    NpcReach Reach(Entity player, int npc);
}

/// <summary>
/// Used where the server does not place the characters itself (a maze, a test): it never knows where they stand.
/// </summary>
public sealed class NoQuestNpcs : IQuestNpcs
{
    public NpcReach Reach(Entity player, int npc) => NpcReach.Unknown;
}
