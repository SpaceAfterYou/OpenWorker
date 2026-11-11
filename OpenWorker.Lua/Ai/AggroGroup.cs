namespace OpenWorker.Lua.Ai;

/// <summary>
/// The group a spawn box puts its monsters in: when one of them fights, the others within the distance that have no enemy join
/// it, up to the count.
/// </summary>
internal readonly record struct AggroGroup(int Id, float Distance, int MaxCount);
