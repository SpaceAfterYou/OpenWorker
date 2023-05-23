using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SoulWorkerResearch.SoulCore.IO.Net.Messages.Server.Login;
using DefaultEcs;
using SoulWorkerResearch.SoulCore.DataTypes;
using SoulWorkerResearch.SoulCore.Abstractions.DataTypes;
using Microsoft.AspNetCore.Authorization;
using OpenWorker.Domain.DatabaseModel;
using OpenWorker.Infrastructure.Communication.HotSpot.Handlers.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Realm.Components;
using OpenWorker.Services.Gate.Infrastructure.Gameplay.Abstractions;
using OpenWorker.Infrastructure.Gameplay.Service.Abstractions;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Handlers.HotSpot;

[AllowAnonymous]
public sealed class EnterServerHandler : IHotSpotHandler<LoginEnterServerMessage>
{
    private readonly IAuthService _authService;
    private readonly IServerService _serverService;

    public EnterServerHandler(IAuthService service, IServerService serverService)
    {
        _authService = service;
        _serverService = serverService;
    }

    public async ValueTask OnHandleAsync(Entity entity, LoginEnterServerMessage request, CancellationToken ct)
    {
        if (!entity.Has<AccountComponent>())
        {
            return;
        }

        await _authService.JoinAsync(request.Account, request.LastGate, request.Key, ct);
        await _serverService.SyncDateTime(ct);

        //entity.Set(new UserInfo { AccountId = model.Id, BackgroundId = model.Background });
        //entity.Set(CreatePersonList(model, request.LastGate));
    }

    //private async ValueTask<bool> IsSessionExists(int acountId, ulong sessionKey, CancellationToken ct)
    //{
    //    var response = await _sessions.ContainsAsync(new SessionContainsRequest { Account = acountId, Key = sessionKey }, cancellationToken: ct);
    //    return response.Result;
    //}


    //private static PersonList CreatePersonList(AccountModel model, ushort gateId)
    //{
    //    var stopwatch = Stopwatch.StartNew();

    //    var characters = model.Person
    //        .Where(p => p.Gate == gateId)
    //        .Select(p => KeyValuePair.Create(p.Id, new CharacterValue
    //        {
    //            Id = p.Id,
    //            Name = p.Name,
    //            Photo = p.PhotoId,
    //            Slot = p.Slot,
    //            Advancement = p.Advancement,
    //            Class = p.Class,
    //            Level = p.Level,
    //            Appearance = new AppearanceValue
    //            {
    //                Face = p.Appearance.Face,
    //                Shape = new AppearanceEntryValue
    //                {
    //                    Hair = new HairValue { Style = p.Appearance.Shape.Hair.Style, Color = p.Appearance.Shape.Hair.Color },
    //                    EyeColor = p.Appearance.Shape.EyeColor,
    //                    SkinColor = p.Appearance.Shape.SkinColor,
    //                },
    //                Look = new AppearanceEntryValue
    //                {
    //                    Hair = new HairValue { Style = p.Appearance.Look.Hair.Style, Color = p.Appearance.Look.Hair.Color },
    //                    EyeColor = p.Appearance.Look.EyeColor,
    //                    SkinColor = p.Appearance.Look.SkinColor,
    //                }
    //            },
    //        }))
    //        .Cast<KeyValuePair<int, ICharacterValue>>();

    //    return new PersonList(model, stopwatch, characters);
    //}
}
