using OpenWorker.GameServer.SoulWorker.Network.DataTypes;

namespace OpenWorker.Hotspot.Handler;

public sealed class HandlerTable(HandlerTableBuilder builder)
{
    private Handler[] Collection { get; } = builder.Build();

    /// <summary>
    /// How many classes of handler the table names, the empty handler among them: the size of the instances of an executor.
    /// </summary>
    internal int Classes => field == 0 ? field = CountClasses() : field;

    private int CountClasses()
    {
        var count = 0;

        foreach (var handler in Collection)
        {
            count = Math.Max(count, handler.Instance + 1);
        }

        return count;
    }

    internal Handler this[MessageOpcode key] => Collection[key];

    /// <summary>
    /// The table is indexed by the two bytes of the opcode read as a short, so an opcode with a command
    /// from 0x80 up, or the last value, falls outside it. The client can send those.
    /// </summary>
    internal bool TryGet(MessageOpcode key, out Handler handler)
    {
        short index = key;

        if ((uint)index < (uint)Collection.Length)
        {
            handler = Collection[index];

            return true;
        }

        handler = default;

        return false;
    }
}