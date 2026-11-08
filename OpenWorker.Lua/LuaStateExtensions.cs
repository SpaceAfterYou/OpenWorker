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
    public static async ValueTask OnEnterPlayerAsync(this LuaState state, ActorValue actor, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventEnterPlayer(actor.Identifier);
        
        if (state.Environment["OnEnterPlayer"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(actor), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnInteractionObject(this LuaState state, int box, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventInteractionObject(box);
        
        if (state.Environment["OnInteractionObject"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(box), new LuaValue(maze)], cancellationToken);
        }
    }
    public static async ValueTask OnCompleteSector(this LuaState state, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventCompleteSector(sector);
        
        if (state.Environment["OnCompleteSector"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(sector), new LuaValue(maze)], cancellationToken);
        }
    }
    
    /// <summary>
    /// A cutscene is over, once for every cutscene the maze has seen.
    /// </summary>
    public static async ValueTask OnCutsceneOff(this LuaState state, string name, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment["OnCutsceneOff"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(name), new LuaValue(maze)], cancellationToken);
        }
    }

    public static async ValueTask OnLuaFunction(this LuaState state, int function, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventLuaFunction(function);
        
        if (state.Environment["LuaFunction"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(function), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnTimerExComplete(this LuaState state, string function, int param1, int param2, int param3, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventTimerExComplete(function, param1, param2, param3);
        
        if (state.Environment[function].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(param1), new LuaValue(param2), new LuaValue(param3), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnTimerComplete(this LuaState state, int timer, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventTimerComplete(timer);
        
        if (state.Environment["OnTimerComplete"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(timer), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnUpdateQuest(this LuaState state, int user, QuestUpdateType type, int identifier, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventUpdateQuest(user, type, identifier);
        
        if (state.Environment["OnUpdateQuest"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(user), new LuaValue((int)type), new LuaValue(identifier), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnActionSkill(this LuaState state, int skill, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventActionSkill(skill);
        
        if (state.Environment["OnActionSkill"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(skill), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask CheckConditionAsync(this LuaState state, int talk, int operation, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventCheckCondition(talk, operation);
        
        if (state.Environment["CheckCondition"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(talk), new LuaValue(operation), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnPartyQuestEvent(this LuaState state, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventPartyQuest(sector);
        
        if (state.Environment["OnPartyQuestEvent"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(sector), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnNpcRotation(this LuaState state, int npc, int key, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        maze.Logger.EventNpcRotation(npc, key);
        
        if (state.Environment["OnNpcRotation"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(npc), new LuaValue(key), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnNpcWayPoint(this LuaState state, int npc, int waypoint, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment["OnNpcWayPoint"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(npc), new LuaValue(waypoint), new LuaValue(maze)], cancellationToken);
        }
    }
    
    public static async ValueTask OnCompleteSpawnStepCondition(this LuaState state, int sector, int step, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment["OnCompleteSpawnStepCondition"].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(sector), new LuaValue(step), new LuaValue(maze)], cancellationToken);
        }
    }
    
    /// <summary>
    /// Scripts compare both ids with strings.
    /// </summary>
    public static async ValueTask OnDie(this LuaState state, int monster, int box, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        await CallDieAsync(state, "OnDie", monster, box, maze, cancellationToken);
        await CallDieAsync(state, "OnRealDie", monster, box, maze, cancellationToken);
    }

    private static async ValueTask CallDieAsync(LuaState state, string name, int monster, int box, LuaMaze maze, CancellationToken cancellationToken)
    {
        if (state.Environment[name].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(monster.ToString(CultureInfo.InvariantCulture)), new LuaValue(box.ToString(CultureInfo.InvariantCulture)), new LuaValue(maze)], cancellationToken);
        }
    }

    public static async ValueTask OnExecuteLuaFunction(this LuaState state, string name, int index, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment[name].TryRead<LuaFunction>(out var callable))
        {
            await state
                .CallAsync(callable, [new LuaValue(index), new LuaValue(maze)], cancellationToken);
        }
    }
}