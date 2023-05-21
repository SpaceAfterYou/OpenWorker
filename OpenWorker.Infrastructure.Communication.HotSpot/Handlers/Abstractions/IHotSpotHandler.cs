using DefaultEcs;
using SoulWorkerResearch.SoulCore.IO.Net.Messages;

namespace OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;

public interface IHotSpotHandler<T> where T : IBinaryMessage
{
    ValueTask OnHandleAsync(Entity entity, T request, CancellationToken ct);
}
