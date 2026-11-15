using Microsoft.Extensions.Configuration;

namespace OpenWorker.Extensions.Data;

/// <summary>
/// A folder of game data a server reads, and what it cannot do without it.
/// </summary>
/// <param name="Folder">The folder.</param>
/// <param name="Without">What does not work without it, in game terms.</param>
/// <param name="Refuse">The server does not start without it: what it would do is not the game.</param>
/// <param name="When">The setting under which the server reads the folder at all; read always when null.</param>
public sealed record DataNeed(DataFolder Folder, string Without, bool Refuse = false, Func<IConfiguration, bool>? When = null);
