using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Npcs.Components;

/// <summary>
/// The credit of the person with the groups of NPCs, null until it is loaded at the entry into a city.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct NpcCreditComponent
{
    public int Person;

    public NpcCreditBook? Book;
}
