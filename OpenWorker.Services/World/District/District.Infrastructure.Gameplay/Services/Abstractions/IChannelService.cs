namespace OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;

public interface IChannelService
{
    ValueTask Join(ushort id, CancellationToken ct = default);
    ValueTask Leave(CancellationToken ct = default);
    ValueTask ShowList(CancellationToken ct = default);
    ValueTask Switch(ushort id, CancellationToken ct = default);
}
