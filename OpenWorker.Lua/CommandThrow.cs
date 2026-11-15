namespace OpenWorker.Lua;

/// <summary>
/// What came of a command that has the nearest monster throw the player.
/// </summary>
/// <param name="Monster">The row of the nearest monster, zero when none stands near.</param>
/// <param name="Skill">The skill whose blow threw the player, zero when the monster has none that throws.</param>
/// <param name="Trigger">The attack of that skill.</param>
/// <param name="Hit">Whether the blow reached the player.</param>
public readonly record struct CommandThrow(int Monster, int Skill, int Trigger, bool Hit);
