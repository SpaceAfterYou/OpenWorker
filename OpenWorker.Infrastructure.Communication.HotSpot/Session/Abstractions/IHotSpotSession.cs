using DefaultEcs;
using SoulWorkerResearch.SoulCore.IO.Net.Messages;

namespace OpenWorker.Infrastructure.Communication.HotSpot.Session.Abstractions;

public interface IHotSpotSession
{
    Entity Entity { get; }

    ValueTask<bool> SendAsync<T>(T message, CancellationToken ct = default) where T : IBinaryOutcomingMessage;
}
