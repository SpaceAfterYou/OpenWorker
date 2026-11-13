using Arch.Core;
using Lua;
using OpenWorker.Gameplay.Modules.HiddenEvents;

namespace OpenWorker.Lua;

/// <summary>
/// What a kill counts for. A monster that dies in a maze counts for every player in the maze: the quests that hunt it, its
/// group and the maze, the daily missions and the achievements of each of them, not only of the one who landed the blow.
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

        var heard = _members.Select(e => e.Player).ToList();

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
    }
}
