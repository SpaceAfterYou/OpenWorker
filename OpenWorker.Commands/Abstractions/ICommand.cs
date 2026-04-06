using Arch.Core;

namespace OpenWorker.Commands.Abstractions;

internal interface ICommand
{
    ValueTask<bool> TryExecute(Entity player, string[] tokens);
}
