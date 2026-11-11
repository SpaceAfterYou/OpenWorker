using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Persons.Components;

/// <summary>
/// The titles a person has opened, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct OpenTitleComponent
{
    public int Person;

    public OpenTitleBook? Book;
}
