using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Hotspot;
using OpenWorker.Navigation;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// What every maze needs to give its monsters a mind. Everything in it is shared and does not change.
/// </summary>
public sealed class CreatureServices(AiScriptLoader scripts, MonsterBehaviorTables behaviors, SkillActionProvider actions, SimulationClock clock, NavMeshProvider meshes,
    PlayerGauges? gauges = null)
{
    /// <summary>
    /// Steps every maze of the server, its monsters with it, and runs what the mazes wait for.
    /// </summary>
    public SimulationClock Clock { get; } = clock;

    public AiScriptLoader Scripts { get; } = scripts;
    public MonsterBehaviorTables Behaviors { get; } = behaviors;
    public SkillActionProvider Actions { get; } = actions;
    public NavMeshProvider Meshes { get; } = meshes;

    /// <summary>
    /// The soul gauge and the stamina of the players: they fill as the maze steps, hits fill the soul gauge, a revival fills
    /// both. A maze without it keeps no gauges.
    /// </summary>
    public PlayerGauges? Gauges { get; } = gauges;
}
