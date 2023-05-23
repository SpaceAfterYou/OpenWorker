using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot.Account;

public sealed class ChangeBackgroundHandler : IHotSpotHandler<CharacterBackgroundChangeServerMessage>
{
    private readonly IAccountService _service;

    public ChangeBackgroundHandler(IAccountService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, CharacterBackgroundChangeServerMessage message, CancellationToken ct) =>
        _service.ChangeBackgroundAsync(message.BackgroundId, ct);
}
