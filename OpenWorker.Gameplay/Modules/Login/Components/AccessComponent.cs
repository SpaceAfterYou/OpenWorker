using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Gameplay.Modules.Login.Components;

/// <summary>
/// What the account of the player may do beyond playing, read from the account when the person enters.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct AccessComponent(AccessLevel Level);
