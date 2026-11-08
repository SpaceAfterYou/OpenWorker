using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Lua.Enums;

namespace OpenWorker.Lua;

/// <summary>
/// What the maze scripts and the server tell each other, as log messages.
/// Calls that only trace are Debug; a call the server could not do as asked is a Warning;
/// a failure inside a delayed action is an Error, because nobody waits for it and the log is the only place it shows.
/// </summary>
internal static partial class LuaLog
{
    // ---- The server calls the script -------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnEnterPlayer: actor {Actor}")]
    public static partial void EventEnterPlayer(this ILogger logger, int actor);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnInteractionObject: box {Box}")]
    public static partial void EventInteractionObject(this ILogger logger, int box);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnCompleteSector: sector {Sector}")]
    public static partial void EventCompleteSector(this ILogger logger, int sector);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event LuaFunction: function {Function}")]
    public static partial void EventLuaFunction(this ILogger logger, int function);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event {Function} of a timer: {Param1}, {Param2}, {Param3}")]
    public static partial void EventTimerExComplete(this ILogger logger, string function, int param1, int param2, int param3);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnTimerComplete: timer {Timer}")]
    public static partial void EventTimerComplete(this ILogger logger, int timer);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnUpdateQuest: user {User}, type {Type}, quest {Quest}")]
    public static partial void EventUpdateQuest(this ILogger logger, int user, QuestUpdateType type, int quest);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnActionSkill: skill {Skill}")]
    public static partial void EventActionSkill(this ILogger logger, int skill);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event CheckCondition: talk {Talk}, operation {Operation}")]
    public static partial void EventCheckCondition(this ILogger logger, int talk, int operation);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnPartyQuestEvent: sector {Sector}")]
    public static partial void EventPartyQuest(this ILogger logger, int sector);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script event OnNpcRotation: npc {Npc}, key {Key}")]
    public static partial void EventNpcRotation(this ILogger logger, int npc, int key);

    // ---- The script calls the server: done ---------------------------------------------------

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call AddTimer: timer {Timer} in {Seconds} s")]
    public static partial void CallAddTimer(this ILogger logger, int timer, float seconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call AddTimerEx: {Function} in {Seconds} s with {Param1}, {Param2}, {Param3}")]
    public static partial void CallAddTimerEx(this ILogger logger, string function, float seconds, int param1, int param2, int param3);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call ApplyBuff: actor {Actor}, buff {Buff}")]
    public static partial void CallApplyBuff(this ILogger logger, int actor, int buff);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call ExcutEventSpawnLua: spawn box {Box}")]
    public static partial void CallExecuteEventSpawn(this ILogger logger, int box);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call GetCommonPositionBox: box {Box}")]
    public static partial void CallGetCommonPositionBox(this ILogger logger, int box);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call MoveNpcToWayPoint: npc {Npc}, waypoint {Waypoint}")]
    public static partial void CallMoveNpcToWayPoint(this ILogger logger, int npc, int waypoint);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Script call SetMazeState: {State}")]
    public static partial void CallSetMazeState(this ILogger logger, MazeState state);

    // ---- The script calls the server: ignored, because the server does not do it yet --------

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call AddEventTimer: {Function}, step {Step}, ready {Ready}, type {Type}, {Seconds} s, {Param1}, {Param2}")]
    public static partial void IgnoredAddEventTimer(this ILogger logger, string function, string step, string ready, int type, float seconds, int param1, int param2);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call AddLuaValue: index {Index}, value {Value}")]
    public static partial void IgnoredAddLuaValue(this ILogger logger, int index, int value);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call CompleteTimerStep: {Function}, step {Step}")]
    public static partial void IgnoredCompleteTimerStep(this ILogger logger, string function, int step);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call ExcutEventSpawnGroupLua: group {Group}")]
    public static partial void IgnoredExecuteEventSpawnGroup(this ILogger logger, int group);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call ExecuteDestroy: type {Type}, table {Table}, animation {Animation}, suicide {Suicide}")]
    public static partial void IgnoredExecuteDestroy(this ILogger logger, int type, string table, string animation, bool suicide);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call GetLuaValue: index {Index}")]
    public static partial void IgnoredGetLuaValue(this ILogger logger, int index);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call LuaClientSync: type {Type}, value {Value}, sector {Sector}, {Seconds} s")]
    public static partial void IgnoredLuaClientSync(this ILogger logger, int type, int value, int sector, float seconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call PrivateSetLuaValue: key {Key}")]
    public static partial void IgnoredSetLuaValueByKey(this ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call PrivateSetLuaValue: index {Index}, value {Value}")]
    public static partial void IgnoredSetLuaValueByIndex(this ILogger logger, int index, int value);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call SetTrapLifeTime: trap {Trap}, {Seconds} s")]
    public static partial void IgnoredSetTrapLifeTime(this ILogger logger, int trap, float seconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call StartEventTimer: {Function}")]
    public static partial void IgnoredStartEventTimer(this ILogger logger, string function);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call CreateTrap: at {X}/{Y}/{Z}, skill {Skill}")]
    public static partial void IgnoredCreateTrap(this ILogger logger, float x, float y, float z, int skill);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call CreateTrapEx: box {Box}, skill {Skill}")]
    public static partial void IgnoredCreateTrapEx(this ILogger logger, int box, int skill);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call CreateTrapExLoop: box {Box}, skill {Skill}, index {Index}")]
    public static partial void IgnoredCreateTrapExLoop(this ILogger logger, int box, int skill, int index);

    // ---- The script asks for something that is not there ---------------------------------------

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script set the unknown maze state {State}")]
    public static partial void UnknownMazeState(this ILogger logger, int state);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script moves npc {Npc} to waypoint {Waypoint}, but the maze has no such creature")]
    public static partial void UnknownNpcToMove(this ILogger logger, int npc, int waypoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Spawn box {Box} has the monster {Monster} of the unsupported type {Type}; it is left out")]
    public static partial void UnsupportedMonsterType(this ILogger logger, int box, int monster, MonsterSpawnType type);

    // ---- Delayed work of a maze ------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Debug, Message = "Spawn box {Box} with the layers {Layers}")]
    public static partial void SpawnBox(this ILogger logger, int box, LayerBitmask layers);

    [LoggerMessage(Level = LogLevel.Error, Message = "The flight of a projectile of skill {Skill} failed")]
    public static partial void ProjectileFlightFailed(this ILogger logger, Exception exception, int skill);

    [LoggerMessage(Level = LogLevel.Error, Message = "A delayed action of the maze failed")]
    public static partial void DelayedActionFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Showing the creatures of spawn box {Box} later failed")]
    public static partial void LateShowFailed(this ILogger logger, Exception exception, int box);
}
