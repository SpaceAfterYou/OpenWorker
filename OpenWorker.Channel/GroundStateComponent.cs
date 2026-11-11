using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Channel;

/// <summary>
/// Whether the client of a player says it stands on the ground, and the height of the ground it stands on (05/40). The
/// server keeps what the client last said and trusts it as it does the places of the moves. The default value is a player
/// that has not said anything yet. Touched on the world loop only.
/// </summary>
/// <param name="OnGround">The player stands on the ground, not in the air.</param>
/// <param name="Height">The height of the ground under the player when it said so.</param>
[EntityComponent(EntityComponentService.All)]
public readonly record struct GroundStateComponent(bool OnGround, float Height);
