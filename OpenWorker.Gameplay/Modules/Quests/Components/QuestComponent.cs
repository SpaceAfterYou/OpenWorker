using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Quests.Components;

/// <summary>
/// The quests of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
[EntityComponent(EntityComponentService.Maze)]
public struct QuestComponent
{
    public int Person;

    public QuestBook? Book;
}
