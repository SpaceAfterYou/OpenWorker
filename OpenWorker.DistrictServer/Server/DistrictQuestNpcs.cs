using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Quests;

namespace OpenWorker.DistrictServer.Server;

/// <summary>
/// Where the characters of this district stand, for the quests: the client says the person talked to a character, and
/// the district knows where the characters its map places are.
/// </summary>
public sealed class DistrictQuestNpcs(World world, NpcManager npcs) : IQuestNpcs
{
    /// <summary>
    /// How far from a character, on the ground, a person may stand and still be talking to it. The client opens the talk
    /// closer than this; the rest is room for a move the server has not heard yet.
    /// </summary>
    public const float TalkReach = 1500f;

    public NpcReach Reach(Entity player, int npc)
    {
        if (!world.IsAlive(player) || !world.TryGet<WorldComponent>(player, out var place))
        {
            return NpcReach.Unknown;
        }

        var found = false;

        foreach (var entity in npcs.Collection)
        {
            if (!world.IsAlive(entity) || !world.TryGet<NpcComponent>(entity, out var character) || character is null || character.Prototype != npc)
            {
                continue;
            }

            found = true;

            if (Vector2.Distance(new Vector2(place.Position.X, place.Position.Y), new Vector2(character.Position.X, character.Position.Y)) <= TalkReach)
            {
                return NpcReach.Near;
            }
        }

        return found ? NpcReach.Far : NpcReach.Unknown;
    }
}
