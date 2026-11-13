using System.Globalization;
using Lua;
using OpenWorker.Domain.Types;
using OpenWorker.Lua.Enums;

namespace OpenWorker.Lua;

/* 19033 */
// struct ST_LUASTRING
// {
//     char strValue[30];
// };

/* 19046 */
// struct __cppobj ST_LUAVALUES
// {
//     std::vector<ST_LUASTRING> vecInfo;
// };

public static class LuaStateExtensions
{
    /// <summary>
    /// Calls a function of the script, if it has one, unless the maze is over. A script that fails ends the maze for
    /// its player (<see cref="LuaMaze.Fail"/>): the error is logged and goes no further, so it never reaches the loop,
    /// a timer or the error budget of the client.
    /// </summary>
    private static async ValueTask CallAsync(LuaState state, LuaMaze maze, string function, LuaValue[] arguments, CancellationToken cancellationToken)
    {
        if (maze.IsClosed || !state.Environment[function].TryRead<LuaFunction>(out var callable))
        {
            return;
        }

        try
        {
            await state.CallAsync(callable, arguments, cancellationToken);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            maze.Fail(function, e);
        }
    }

    public static async ValueTask OnEnterPlayerAsync(this LuaState state, ActorValue actor, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventEnterPlayer(actor.Identifier);
        
        await CallAsync(state, maze, "OnEnterPlayer", [new LuaValue(actor), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnInteractionObject(this LuaState state, int box, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventInteractionObject(box);
        
        await CallAsync(state, maze, "OnInteractionObject", [new LuaValue(box), new LuaValue(maze)], cancellationToken);
    }
    public static async ValueTask OnCompleteSector(this LuaState state, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventCompleteSector(sector);
        
        await CallAsync(state, maze, "OnCompleteSector", [new LuaValue(sector), new LuaValue(maze)], cancellationToken);
    }
    
    /// <summary>
    /// A game mode of the maze has ended and the script hears how: the function is named for the mode and the result, and gets the
    /// sector the mode was played in.
    /// </summary>
    public static async ValueTask OnModeEnded(this LuaState state, string function, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallAsync(state, maze, function, [new LuaValue(sector), new LuaValue(maze)], cancellationToken);
    }

    /// <summary>
    /// A cutscene is over, once for every cutscene the maze has seen.
    /// </summary>
    public static async ValueTask OnCutsceneOff(this LuaState state, string name, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallAsync(state, maze, "OnCutsceneOff", [new LuaValue(name), new LuaValue(maze)], cancellationToken);
    }

    public static async ValueTask OnLuaFunction(this LuaState state, int function, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventLuaFunction(function);
        
        await CallAsync(state, maze, "LuaFunction", [new LuaValue(function), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnTimerExComplete(this LuaState state, string function, int param1, int param2, int param3, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventTimerExComplete(function, param1, param2, param3);
        
        await CallAsync(state, maze, function, [new LuaValue(param1), new LuaValue(param2), new LuaValue(param3), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnTimerComplete(this LuaState state, int timer, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventTimerComplete(timer);
        
        await CallAsync(state, maze, "OnTimerComplete", [new LuaValue(timer), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnUpdateQuest(this LuaState state, int user, QuestUpdateType type, int identifier, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventUpdateQuest(user, type, identifier);
        
        await CallAsync(state, maze, "OnUpdateQuest", [new LuaValue(user), new LuaValue((int)type), new LuaValue(identifier), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnActionSkill(this LuaState state, int skill, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventActionSkill(skill);
        
        await CallAsync(state, maze, "OnActionSkill", [new LuaValue(skill), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask CheckConditionAsync(this LuaState state, int talk, int operation, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventCheckCondition(talk, operation);
        
        await CallAsync(state, maze, "CheckCondition", [new LuaValue(talk), new LuaValue(operation), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnPartyQuestEvent(this LuaState state, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventPartyQuest(sector);
        
        await CallAsync(state, maze, "OnPartyQuestEvent", [new LuaValue(sector), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnNpcRotation(this LuaState state, int npc, int key, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventNpcRotation(npc, key);
        
        await CallAsync(state, maze, "OnNpcRotation", [new LuaValue(npc), new LuaValue(key), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnNpcWayPoint(this LuaState state, int npc, int waypoint, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallAsync(state, maze, "OnNpcWayPoint", [new LuaValue(npc), new LuaValue(waypoint), new LuaValue(maze)], cancellationToken);
    }
    
    public static async ValueTask OnCompleteSpawnStepCondition(this LuaState state, int sector, int step, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallAsync(state, maze, "OnCompleteSpawnStepCondition", [new LuaValue(sector), new LuaValue(step), new LuaValue(maze)], cancellationToken);
    }
    
    /// <summary>
    /// Scripts compare both ids with strings.
    /// </summary>
    public static async ValueTask OnDie(this LuaState state, int monster, int box, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallDieAsync(state, "OnDie", monster, box, maze, cancellationToken);
        await CallDieAsync(state, "OnRealDie", monster, box, maze, cancellationToken);

        if (!maze.IsClosed && maze.TakeFallenGroup(box) is var group and > 0)
        {
            await CallAsync(state, maze, "OnRealDieEx", [new LuaValue(group), new LuaValue(maze)], cancellationToken);
        }
    }

    private static async ValueTask CallDieAsync(LuaState state, string name, int monster, int box, LuaMaze maze, CancellationToken cancellationToken)
    {
        await CallAsync(state, maze, name, [new LuaValue(monster.ToString(CultureInfo.InvariantCulture)), new LuaValue(box.ToString(CultureInfo.InvariantCulture)), new LuaValue(maze)], cancellationToken);
    }

    public static async ValueTask OnExecuteLuaFunction(this LuaState state, string name, int index, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallAsync(state, maze, name, [new LuaValue(index), new LuaValue(maze)], cancellationToken);
    }
}