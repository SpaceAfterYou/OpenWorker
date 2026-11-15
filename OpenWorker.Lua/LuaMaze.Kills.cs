using Arch.Core;
using Lua;
using OpenWorker.Gameplay.Modules.HiddenEvents;

namespace OpenWorker.Lua;

/// <summary>
/// What a kill counts for. A monster that dies in a maze counts for every player in the maze: the quests that hunt it, its
/// group and the maze, the daily missions and the achievements of each of them, not only of the one who landed the blow. In an
/// open maze of the world it counts for the killer, and its quests and achievements for the members of the killer's party in
/// the same instance.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// Tells the rest of the game that a monster died, for each player who is in the maze and for the killer, who may have left
    /// while a script ran.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask ReportKillAsync(Entity killer, int monster, uint conditions)
    {
        CountHiddenEvent(HiddenEventTarget.Monster, monster, 1);

        if (Ecs.IsAlive(killer))
        {
            reports.MonsterSlain(killer, monster);
        }

        // In an open maze of the world a kill is the killer's alone.
        var heard = IsWorld ? new List<Entity>() : _members.Select(e => e.Player).ToList();

        if (!heard.Contains(killer))
        {
            heard.Add(killer);
        }

        foreach (var player in heard)
        {
            if (Closed)
            {
                return;
            }

            if (Ecs.IsAlive(player))
            {
                await reports.MonsterKilledAsync(player, monster, conditions);
            }
        }

        if (IsWorld)
        {
            await ShareKillAsync(killer, monster);
        }
    }

    /// <summary>
    /// The members of the killer's party who play this instance count the kill for their quests and achievements.
    /// </summary>
    private async ValueTask ShareKillAsync(Entity killer, int monster)
    {
        if (PartyOf is null || _members.Count < 2 || Closed)
        {
            return;
        }

        var party = await PartyOf(killer);

        foreach (var member in party)
        {
            if (Closed)
            {
                return;
            }

            if (member != killer && Ecs.IsAlive(member) && Find(member) is not null)
            {
                await reports.PartyMonsterKilledAsync(member, monster);
            }
        }
    }
}
