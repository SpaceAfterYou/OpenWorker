using Arch.Core;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The write of a player that a handler awaits, ending as the test says, and counted.
/// </summary>
internal sealed class TestPersonFlush(bool written) : IPersonFlush
{
    /// <summary>
    /// The database takes every write.
    /// </summary>
    public static TestPersonFlush Written => new(true);

    public int Calls { get; private set; }

    public ValueTask<bool> FlushAsync(Entity player)
    {
        Calls++;

        return ValueTask.FromResult(written);
    }
}
