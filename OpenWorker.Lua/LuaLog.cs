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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call CreateTrapExLoop: box {Box}, skill {Skill}, count {Count}")]
    public static partial void IgnoredCreateTrapExLoop(this ILogger logger, int box, int skill, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call ChangeMonster: {Source} -> {Target}")]
    public static partial void IgnoredChangeMonster(this ILogger logger, string source, string target);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call ChangeNpcAnimation: npc {Npc}, animation {Animation}")]
    public static partial void IgnoredChangeNpcAnimation(this ILogger logger, int npc, string animation);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call InitPartyQuest: quest {Quest}, condition {Condition}, sector {Sector}")]
    public static partial void IgnoredInitPartyQuest(this ILogger logger, int quest, int condition, int sector);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call SetEscortCondition: condition {Condition}")]
    public static partial void IgnoredSetEscortCondition(this ILogger logger, int condition);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call SetEscortMonster")]
    public static partial void IgnoredSetEscortMonster(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call SetMonsterAllowPassiveType: monster {Monster}, type {Type}")]
    public static partial void IgnoredSetMonsterAllowPassiveType(this ILogger logger, int monster, int type);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignored script call SetMonsterDefenceType: monster {Monster}, type {Type}, {Seconds} s")]
    public static partial void IgnoredSetMonsterDefenceType(this ILogger logger, int monster, int type, float seconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script asked for the mode {Call} of the table {Table}, which the server has no row for; nothing starts")]
    public static partial void UnknownMode(this ILogger logger, string call, int table);

    // ---- The script asks for something that is not there ---------------------------------------

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script {Script} called {Function}, which the server does not do; the call does nothing (said once for the script and the function)")]
    public static partial void NotDoneByServer(this ILogger logger, string function, string script);

    [LoggerMessage(Level = LogLevel.Error, Message = "The script {Script} failed in {Function}; the maze ends for the player")]
    public static partial void ScriptFailed(this ILogger logger, Exception exception, string function, string script);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script added an event timer {Function} of the unknown type {Type}; it is left out")]
    public static partial void UnknownEventTimerType(this ILogger logger, int type, string function);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script stored a value under something that is neither a key nor a number; nothing is stored")]
    public static partial void UnreadableLuaValue(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script set the unknown maze state {State}")]
    public static partial void UnknownMazeState(this ILogger logger, int state);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script asked for the position of box {Box}, which the batch of the maze does not have; it gets the origin")]
    public static partial void UnknownPositionBox(this ILogger logger, int box);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script applied the buff {Buff} to actor {Actor}, which the buff table does not have")]
    public static partial void UnknownBuff(this ILogger logger, int actor, int buff);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script moves npc {Npc} to waypoint {Waypoint}, but the maze has no such creature")]
    public static partial void UnknownNpcToMove(this ILogger logger, int npc, int waypoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Script moves npc {Npc} to waypoint {Waypoint}, which the map does not have; it stays")]
    public static partial void UnknownWayPoint(this ILogger logger, int npc, int waypoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Spawn box {Box} has the monster {Monster} of the unsupported type {Type}; it is left out")]
    public static partial void UnsupportedMonsterType(this ILogger logger, int box, int monster, MonsterSpawnType type);

    // ---- Delayed work of a maze ------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Debug, Message = "Spawn box {Box} with the layers {Layers}")]
    public static partial void SpawnBox(this ILogger logger, int box, LayerBitmask layers);

    [LoggerMessage(Level = LogLevel.Error, Message = "The flight of a projectile of skill {Skill} failed")]
    public static partial void ProjectileFlightFailed(this ILogger logger, Exception exception, int skill);

    [LoggerMessage(Level = LogLevel.Error, Message = "The timer function {Function} of the script failed")]
    public static partial void TimerFunctionFailed(this ILogger logger, Exception exception, string function);

    [LoggerMessage(Level = LogLevel.Error, Message = "The timer {Timer} of the script failed")]
    public static partial void TimerFailed(this ILogger logger, Exception exception, int timer);

    [LoggerMessage(Level = LogLevel.Error, Message = "The timers of the maze stopped after a failure")]
    public static partial void TimersFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Playing a skill in the maze failed")]
    public static partial void SkillPlaybackFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "A chain between monsters failed")]
    public static partial void ChainFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "A delayed action of the maze failed")]
    public static partial void DelayedActionFailed(this ILogger logger, Exception exception);

    // ---- Monsters ---------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Warning, Message = "No script of the AI named {Name}, the monster will not think")]
    public static partial void NoAiScript(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The script of the AI named {Name} cannot be read, the monster will not think")]
    public static partial void AiScriptUnreadable(this ILogger logger, Exception exception, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Script}: {Method} was called with something it cannot take: {Message}")]
    public static partial void AiScriptBadArgument(this ILogger logger, string script, string method, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "The monster {Monster} failed to think")]
    public static partial void MonsterFailedToThink(this ILogger logger, Exception exception, int monster);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Monster {Monster} ({Actor}): {Old} -> {New}")]
    public static partial void MonsterChangedState(this ILogger logger, int monster, int actor, OpenWorker.Gameplay.Ai.AiState old, OpenWorker.Gameplay.Ai.AiState @new);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Monster {Monster} wants to die, which a maze script has to finish")]
    public static partial void MonsterWantsToDie(this ILogger logger, int monster);
}
