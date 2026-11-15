using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Maze.Components;

/// <summary>
/// The channel of the open maze of the world (District 6) the player is in, which the others see as its channel.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct WorldMazeChannelComponent(byte Channel);
