using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Types;

/// <summary>
/// An object of the maze a player can use: shown or not, usable or not, its box and how many times it was used.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct MazeInteractionValue(bool Show, bool Enable, int Box, int CallCount);
