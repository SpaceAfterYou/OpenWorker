using OpenWorker.GameServer.SoulWorker.Network.DataTypes;

namespace OpenWorker.Hotspot.Handler;

public sealed class HandlerCollection(HandlerCollectionBuilder builder)
{
    private Handler[] Collection { get; } = builder.Build();

    internal Handler this[MessageOpcode key] => Collection[key];
}