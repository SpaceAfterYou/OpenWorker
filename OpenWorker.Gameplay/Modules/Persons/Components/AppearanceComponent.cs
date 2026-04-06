using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Persons.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct AppearanceComponent
{
    public required AppearanceComponentEntry HairStyle { get; init; }
    public required AppearanceComponentEntry HairColor { get; init; }
    public required AppearanceComponentEntry EyeColor { get; init; }
    public required AppearanceComponentEntry SkinColor { get; init; }
}