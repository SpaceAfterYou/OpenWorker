using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// What every maze needs to give its monsters a mind. Everything in it is shared and does not change.
/// </summary>
public sealed class CreatureServices(AiScriptLoader scripts, MonsterBehaviorTables behaviors, SkillActionProvider actions)
{
    public AiScriptLoader Scripts { get; } = scripts;
    public MonsterBehaviorTables Behaviors { get; } = behaviors;
    public SkillActionProvider Actions { get; } = actions;
}
