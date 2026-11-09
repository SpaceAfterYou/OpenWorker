using Arch.Core;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Commands.Abstractions;

internal interface ICommand
{
    /// <summary>
    /// The access level the account of the player needs for the command: a game master's unless the command says otherwise.
    /// </summary>
    AccessLevel Required => AccessLevel.GameMaster;

    ValueTask<bool> TryExecute(Entity player, string[] tokens);
}
