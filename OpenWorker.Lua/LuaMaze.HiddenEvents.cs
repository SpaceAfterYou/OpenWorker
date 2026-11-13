using Arch.Core;
using Lua;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Lua;

/// <summary>
/// The hidden event of the maze: one at most, started by a box of the map, shared by every player in the maze. Every change is
/// told to all of them (11/37), a player who comes later is told where it stands, and the clear decides it.
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// The hidden events of the maps, or nothing for a maze that has none.
    /// </summary>
    public HiddenEventCatalog? HiddenEvents { get; init; }

    /// <summary>
    /// The hidden event of the maze, once a box has started one.
    /// </summary>
    public HiddenEvent? HiddenEvent { get; private set; }

    /// <summary>
    /// What the met hidden event gives each player at the clear, else nothing.
    /// </summary>
    public HiddenEventReward? HiddenEventReward => IsComplete ? HiddenEvent?.Reward : null;

    /// <summary>
    /// The player stepped into the box of a hidden event for the first time: while the maze has none, the box may start one.
    /// </summary>
    [LuaIgnoreMember]
    public void OnHiddenEventBox(Entity player, int box, float rate)
    {
        if (Closed || IsComplete || HiddenEvent is not null || HiddenEvents?.TryStart(player, box, rate) is not { } started)
        {
            return;
        }

        HiddenEvent = started;

        creatureManager.Broadcast(HiddenEventMessage(started));
        started.Begin();
    }

    /// <summary>
    /// Something the condition of the hidden event may count has happened in the maze.
    /// </summary>
    [LuaIgnoreMember]
    public void CountHiddenEvent(HiddenEventTarget target, int what, int amount)
    {
        if (!Closed && !IsComplete && HiddenEvent is { } running && running.Count(target, what, amount))
        {
            creatureManager.Broadcast(HiddenEventMessage(running));
        }
    }

    /// <summary>
    /// A skill a player starts counts for the kinds of skill the condition names.
    /// </summary>
    private void CountHiddenEventSkill(int skill)
    {
        if (HiddenEvent is null || HiddenEvents is null)
        {
            return;
        }

        foreach (var target in HiddenEvents.SkillTargets(skill))
        {
            CountHiddenEvent(target, 0, 1);
        }
    }

    /// <summary>
    /// A skill the player started counts when it was used linked from the row of the deck the client names.
    /// </summary>
    [LuaIgnoreMember]
    public void CountHiddenEventDeckSkill(Entity player, int skill, byte row)
    {
        if (HiddenEvent is not null && HiddenEvents is not null && Ecs.IsAlive(player) &&
            Ecs.TryGet(player, out SkillDeckComponent deck) && Ecs.TryGet(player, out SkillDeckBonusComponent bonus) &&
            HiddenEvents.IsDeckLink(deck.SlotList, bonus.Values, skill, row))
        {
            CountHiddenEvent(HiddenEventTarget.SkillLink, 0, 1);
        }
    }

    /// <summary>
    /// A blow of a player on a monster: the armor it broke, the monster it killed in the air or with a jump attack. The blows of
    /// helpers count nothing.
    /// </summary>
    internal void CountHiddenEventBlow(Entity attacker, Entity target, int skill, bool armorWasBroken, bool killed, bool aloft)
    {
        if (HiddenEvent is null || HiddenEvents is null || Ecs.Has<MonsterCombatComponent>(attacker))
        {
            return;
        }

        var brokeArmor = !armorWasBroken && Ecs.TryGet(target, out MonsterCombatComponent body) && body.SuperArmorBrokenUntil is not null;

        foreach (var counted in HiddenEvents.BlowTargets(skill, brokeArmor, killed, aloft))
        {
            CountHiddenEvent(counted, 0, 1);
        }
    }

    /// <summary>
    /// Whether the hidden event of the maze has been met.
    /// </summary>
    private bool HiddenEventMet => HiddenEvent is { State: HiddenEventState.Met };

    /// <summary>
    /// The maze is cleared: the hidden event still going is decided, before the rewards of the clear are given.
    /// </summary>
    private void DecideHiddenEvent()
    {
        if (HiddenEvent is { } running && running.Finish())
        {
            creatureManager.Broadcast(HiddenEventMessage(running));
        }
    }

    /// <summary>
    /// A player whose client has loaded the maze hears of the hidden event already going: that it started, then where it stands.
    /// </summary>
    private void TellHiddenEvent(Entity player)
    {
        if (HiddenEvent is not { } running || !Ecs.IsAlive(player) || !Ecs.TryGet(player, out ServerSessionComponent session))
        {
            return;
        }

        session.Send(HiddenEventMessage(running) with { State = (byte)HiddenEventState.Started, Count = 0 });
        session.Send(HiddenEventMessage(running));
    }

    private static MazeHiddenEventResponse HiddenEventMessage(HiddenEvent running)
    {
        return new MazeHiddenEventResponse
        {
            Event = running.Id,
            Condition = running.Condition.Id,
            State = (byte)running.State,
            Count = running.Counted
        };
    }
}
