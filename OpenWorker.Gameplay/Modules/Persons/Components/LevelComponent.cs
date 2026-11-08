using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Persons.Components;

/// <summary>
/// The level a character has reached and the experience it has gathered towards the next one. The echelon is another
/// thing: <see cref="Messages.Response.Person.Components.RankComponent"/>.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct LevelComponent(byte Level, int Experience);
