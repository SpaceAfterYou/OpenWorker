using SoulWorkerResearch.SoulCore.Defines;

namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IChatService
{
    ValueTask BroadcastNormal(ChatType type, string message, CancellationToken ct = default);
}
