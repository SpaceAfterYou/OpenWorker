using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Items.Components;

/// <summary>
/// The use book of the person, null until it is loaded at the entry.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct ItemUseComponent
{
    public int Person;

    public ItemUseBook? Book;
}
