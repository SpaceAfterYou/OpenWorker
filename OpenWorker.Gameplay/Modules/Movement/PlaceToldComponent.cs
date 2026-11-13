using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Movement;

/// <summary>
/// The server has put the player at a place its client did not ask for, and the pace check has not seen a move since.
/// </summary>
[EntityComponent(EntityComponentService.All)]
public record struct PlaceToldComponent(bool Pending);
