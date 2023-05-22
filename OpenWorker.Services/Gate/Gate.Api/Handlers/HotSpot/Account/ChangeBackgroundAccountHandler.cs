using DefaultEcs;
using Gate.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace Gate.Api.Handlers.HotSpot.Account;

public sealed class ChangeBackgroundAccountHandler : IHotSpotHandler<CharacterBackgroundChangeServerMessage>
{
    private readonly IAccountService _service;

    public ChangeBackgroundAccountHandler(IAccountService service) => _service = service;

    public ValueTask OnHandleAsync(Entity entity, CharacterBackgroundChangeServerMessage message, CancellationToken ct) =>
        _service.ChangeBackground(message.BackgroundId, ct);
}
