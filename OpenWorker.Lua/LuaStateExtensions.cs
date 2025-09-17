using Lua;
using OpenWorker.Domain.Types;

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
        Console.WriteLine($"[{nameof(OnEnterPlayerAsync)}]: {actor}");
        
        if (state.Environment["OnEnterPlayer"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(actor), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnInteractionObject(this LuaState state, int box, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnInteractionObject)}]: {box}");
        
        if (state.Environment["OnInteractionObject"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(box), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    public static async ValueTask OnCompleteSector(this LuaState state, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnCompleteSector)}]: {sector}");
        
        if (state.Environment["OnCompleteSector"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(sector), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnLuaFunction(this LuaState state, int function, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnLuaFunction)}]: {function}");
        
        if (state.Environment["LuaFunction"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(function), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnTimerExComplete(this LuaState state, string function, int param1, int param2, int param3, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnTimerExComplete)}]: {function}, {param1}, {param2}, {param3}");
        
        if (state.Environment[function].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(param1), new LuaValue(param2), new LuaValue(param3), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnTimerComplete(this LuaState state, int timer, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnTimerComplete)}]: {timer}");
        
        if (state.Environment["OnTimerComplete"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(timer), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnUpdateQuest(this LuaState state, int user, QuestUpdateType type, int identifier, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnUpdateQuest)}]: {user}, {type}, {identifier}");
        
        if (state.Environment["OnUpdateQuest"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(user), new LuaValue((int)type), new LuaValue(identifier), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnActionSkill(this LuaState state, int skill, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnActionSkill)}]: {skill}");
        
        if (state.Environment["OnActionSkill"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(skill), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask CheckConditionAsync(this LuaState state, int talk, int operation, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(CheckConditionAsync)}]: {talk}, {operation}");
        
        if (state.Environment["CheckCondition"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(talk), new LuaValue(operation), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnPartyQuestEvent(this LuaState state, int sector, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnPartyQuestEvent)}]: {sector}");
        
        if (state.Environment["OnPartyQuestEvent"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(sector), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnNpcRotation(this LuaState state, int npc, int key, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{nameof(OnNpcRotation)}]: {npc}, {key}");
        
        if (state.Environment["OnNpcRotation"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(npc), new LuaValue(key), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnNpcWayPoint(this LuaState state, int npc, int waypoint, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment["OnNpcWayPoint"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(npc), new LuaValue(waypoint), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnCompleteSpawnStepCondition(this LuaState state, int sector, int step, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment["OnCompleteSpawnStepCondition"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(sector), new LuaValue(step), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnRealDie(this LuaState state, int monster, int box, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment["OnRealDie"].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(monster), new LuaValue(box), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
    
    public static async ValueTask OnExecuteLuaFunction(this LuaState state, string name, int index, LuaMaze maze, CancellationToken cancellationToken = default)
    {
        if (state.Environment[name].TryRead<LuaFunction>(out var callable))
        {
            await callable
                .InvokeAsync(state, [new LuaValue(index), new LuaValue(maze)], cancellationToken)
                .ConfigureAwait(false);
        }
    }
}