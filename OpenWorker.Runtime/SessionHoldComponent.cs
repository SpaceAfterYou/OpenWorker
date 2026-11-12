namespace OpenWorker.Hotspot;

/// <summary>
/// Put on a player whose connection is closing when something of the person is still to be finished after the entity is
/// gone, such as what it changed and the database has not taken yet. The session stays held, so no other server and no
/// new login takes the person, until <see cref="Until"/> ends.
/// </summary>
public readonly record struct SessionHoldComponent(Task Until);
