using Arch.Core;
using Arch.Core.Extensions;
using Lua;
using OpenWorker.Batch;
using OpenWorker.Batch.Entities;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua;

public enum MazeState
{
    None = 0x0,
    Run = 0x1,
    StartBoss = 0x2,
    WaitToDie = 0x3,
    ModeFail = 0x4,
    Wait = 0x5
}

[LuaObject]
public partial class LuaMaze(
    LuaState state, 
    World ecs, 
    Entity player, 
    LuaCreatureManager creatureManager,
    BuffManager buffManager,
    QuestManager questManager,
    VBatchFile batch
) : ILuaUserData
{
    public LuaTable? Metatable { get; set; }
    
    public LuaState State { get; } = state;
    
    public World Ecs { get; } = ecs;
    public VBatchFile Batch { get; } = batch;
    
    [LuaMember]
    public void AcceptQuest(int episode)
    {
        questManager.AcceptEpisode(player, episode);
    }

    [LuaMember]
    public void AddEventMaxTime()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void AddEventTimer(string function, string step, string ready, int type, float time, int param1, int param2)
    {
        Console.WriteLine($"[{nameof(AddEventTimer)}]: {function}, {step}, {ready}, {type}, {time}, {param1}, {param2}");
    }

    [LuaMember]
    public void AddLuaValue(int index, int value)
    {
        Console.WriteLine($"[{nameof(AddLuaValue)}]: {index}, {value}");
    }

    [LuaMember]
    public void AddTimer(int identifier, float time)
    {
        Console.WriteLine($"[{nameof(AddTimer)} start] {identifier} {time}");
        
        _ = Task
            .Delay(TimeSpan.FromSeconds(time))
            .ContinueWith(_ => State.OnTimerComplete(identifier, this))
            .ContinueWith(_ => Console.WriteLine($"[{nameof(AddTimer)} end] {identifier} {time}"));
    }

    [LuaMember]
    public void AddTimerEx(string function, float time, int param1, int param2, int param3)
    {
        Console.WriteLine($"[{nameof(AddTimerEx)} start] {function} {param1} {param2} {param3}");
        
        _ = Task
            .Delay(TimeSpan.FromSeconds(time))
            .ContinueWith(_ => State.OnTimerExComplete(function, param1, param2, param3, this))
            .ContinueWith(_ => Console.WriteLine($"[{nameof(AddTimerEx)} end] {function} {param1} {param2} {param3}"));
    }

    [LuaMember]
    public void AllDestroySectorMonster()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void AllUserWarpInSector()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ApplyBuff(int actor, int buff)
    {
        Console.WriteLine($"[{nameof(ApplyBuff)}]: {actor}, {buff}");
        
        buffManager.Apply(player, actor, buff);
    }

    [LuaMember]
    public void ChangeMonster()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ChangeNpcAnimation()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ChatMessage()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void ClearAllTimers()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CompleteCondition()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void CompleteQuest(int episode)
    {
        questManager.CompleteEpisode(player, episode);
    }

    [LuaMember]
    public void CompleteTimerStep(string function, int step)
    {
        Console.WriteLine($"[{nameof(CompleteTimerStep)}]: {function}, {step}");
    }

    [LuaMember]
    public void EnableInteractionBox(int box, bool enable)
    {
        var action = Batch.EventBox.InterActions.First(x => x.Id == box);

        var session = player.Get<ServerSessionComponent>();
        
        session.Send(new MazeInteractionEnableResponse
        {
            Show = true,
            Enable = enable,
            BoxIndex = action.Interaction,
            CallCount = 1
        });
    }

    [LuaMember("ExcutEventSpawnGroupLua")]
    public void ExecuteEventSpawnGroupLua(int group)
    {
        Console.WriteLine($"[{nameof(ExecuteEventSpawnGroupLua)}]: {group}");
    }

    [LuaMember("ExcutEventSpawnLua")]
    public void ExecuteEventSpawnLua(int box)
    {
        Console.WriteLine($"[{nameof(ExecuteEventSpawnLua)} start]: {box}");
        
        creatureManager.Create(player, box);
        
        Console.WriteLine($"[{nameof(ExecuteEventSpawnLua)} end]: {box}");
    }

    [LuaMember]
    public void ExecuteDestroy(int type, string table, string animation, bool suicide = true)
    {
        Console.WriteLine($"[{nameof(ExecuteDestroy)}]: {type}, {table}, {animation}, {suicide}");
    }
    
    [LuaMember]
    public VVector3 GetCommonPositionBox(int box)
    {
        Console.WriteLine($"[{nameof(GetCommonPositionBox)}]: {box}");

        return Batch.EventBox.CommonPositions
            .First(x => x.Id == box)
            .GetRandomPosition();
    }

    [LuaMember]
    public void GetLuaValue(int index)
    {
        Console.WriteLine($"[{nameof(GetLuaValue)}]: {index}");
    }

    [LuaMember]
    public void InitPartyQuest()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public bool IsHaveCondition(int conditionId)
    {
        return questManager.ActiveEpisodeUsesCondition(player, conditionId);
    }

    [LuaMember]
    public bool IsHaveQuest(int episodeId)
    {
        return questManager.HasActiveEpisode(player, episodeId);
    }

    [LuaMember]
    public void LuaClientSync(int type, int value, int sector = 0, float time = 0)
    {
        Console.WriteLine($"[{nameof(LuaClientSync)}]: {type}, {value}, {sector}, {time}");
    }

    [LuaMember]
    public void MoveNpcToWayPoint(int npc, int waypoint)
    {
        Console.WriteLine($"[{nameof(MoveNpcToWayPoint)}]: {npc}, {waypoint}");
        
        creatureManager.MoveToPoint(npc, waypoint);
    }

    [LuaMember]
    public void RemoveQuestAll()
    {
        questManager.RemoveAllActiveEpisodes(player);
    }

    [LuaMember]
    public void RemoveTimerEx()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SectorClear(int sector)
    {
        Console.WriteLine($"[{nameof(SectorClear)}]: {sector}");
    }

    [LuaMember]
    public void SetEscortCondition()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetEscortMonster()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetGateFlag()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetGateFlagNoSend()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void PrivateSetLuaValue(LuaValue keyOrIndex, LuaValue value)
    {
        if (keyOrIndex.TryRead<string>(out var key))
        {
            PrivateSetLuaValue(key);
            return;
        }
        
        PrivateSetLuaValue(keyOrIndex.Read<int>(), value.Read<int>());
    }
    
    [LuaIgnoreMember]
    private void PrivateSetLuaValue(string key)
    {
        Console.WriteLine($"[{nameof(PrivateSetLuaValue)}]: {key}");
    }
    
    [LuaIgnoreMember]
    private void PrivateSetLuaValue(int index, int value)
    {
        Console.WriteLine($"[{nameof(PrivateSetLuaValue)}]: {index}, {value}");
    }

    /// <summary>
    /// Lua-CSharp is not supports enum as parameter right now
    /// </summary>
    [LuaMember]
    public void SetMazeState(int state)
    {
        if (Enum.IsDefined(typeof(MazeState), state))
        {
            SetMazeState((MazeState)state);
        }
        else
        {
            Console.WriteLine($"[{nameof(SetMazeState)}]: Invalid maze state ID: {state}");
        }
    }

    [LuaIgnoreMember]
    public void SetMazeState(MazeState state)
    {
        Console.WriteLine($"[{nameof(SetMazeState)}]: {state}");
    }

    [LuaMember]
    public void SetMonsterAllowPassiveType()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetMonsterDefenceType()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetNpcRotation()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetPotalFlag(int box, bool flag)
    {
        Console.WriteLine($"[{nameof(SetPotalFlag)}]: {box}, {flag}");
    }

    [LuaMember]
    public void SetSectorStepStop()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void SetTrapLifeTime(int index, float time)
    {
        Console.WriteLine($"[{nameof(SetTrapLifeTime)}]: {index}, {time}");
    }

    [LuaMember]
    public void ShowCasualRaidTimer()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void StartDefenceMode()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void StartEventTimer(string function)
    {
        Console.WriteLine($"[{nameof(StartEventTimer)}]: {function}");
    }

    [LuaMember]
    public void StartOperationMode()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void StartSurvivalMode()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void TerminateSpawn()
    {
        throw new NotImplementedException();
    }

    [LuaMember]
    public void TerminateSpawnBox()
    {
        throw new NotImplementedException();
    }
}