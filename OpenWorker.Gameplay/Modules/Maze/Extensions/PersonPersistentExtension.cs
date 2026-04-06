using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Maze.Components;

namespace OpenWorker.Gameplay.Modules.Maze.Extensions;

public static class PersonPersistentExtension
{
    public static FatiguePointsComponent ToFatiguePointsComponent(this PersonPersistent value)
    {
        return new FatiguePointsComponent
        {
            Bonus = value.FatiguePointBonus,
            Common = value.FatiguePointCommon
        };
    }
}