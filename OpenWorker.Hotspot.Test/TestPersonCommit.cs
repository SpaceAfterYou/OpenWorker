using Arch.Core;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The write of a player that a handler awaits before a store, ending as the test says, and counted.
/// </summary>
internal sealed class TestPersonCommit(bool written) : IPersonCommit
{
    /// <summary>
    /// The database takes every write.
    /// </summary>
    public static TestPersonCommit Written => new(true);

    public int Calls { get; private set; }

    public ValueTask<bool> CommitAsync(Entity player)
    {
        Calls++;

        return ValueTask.FromResult(written);
    }
}
