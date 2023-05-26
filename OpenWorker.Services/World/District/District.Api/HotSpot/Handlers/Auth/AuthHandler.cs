using DefaultEcs;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Service.Abstractions;
using OpenWorker.Services.District.Infrastructure.Gameplay.Services.Abstractions;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Character;

namespace OpenWorker.Services.World.District.Api.HotSpot.Handlers.Chat;

public sealed record AuthHandler : IHotSpotHandler<CharacterEnterGameServerServerMessage>
{
    private readonly IServerService _server;
    private readonly IChannelService _channel;
    private readonly IBoosterService _booster;
    private readonly IRouletteService _roulette;
    private readonly IAttendanceService _attendance;
    private readonly IPostService _post;
    private readonly IMazeService _maze;
    private readonly IBattlePassService _battlePass;
    private readonly IItemService _item;
    private readonly IPersonService _person;

    public async ValueTask OnHandleAsync(Entity entity, CharacterEnterGameServerServerMessage message, CancellationToken ct)
    {
        await _server.SyncDateTime(ct);
        // send world version
        await _battlePass.SendCurrentState(ct);
        await _booster.SendDaily(ct);
        // enter server response
        await _post.Check(0, ct);
        await _post.Check(1, ct);
        await _post.Check(2, ct);
        await _attendance.SendCurrentState(ct);
        await _roulette.SendCurrentState(ct);
        // steel graves state
        await _maze.SendCurrentLimits(ct);
        await _item.SendAkashicCurrentState(ct);

        await _person.LoadPerson(ct);
        await _channel.Join(message.Map.Map.Channel, ct);
        await _person.SyncState(ct);
    }
}
