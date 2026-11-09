using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel.Community;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Login.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Persistence;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The system group in a district and in a maze: the answer to the keep-alive the server sends (<see cref="KeepAliveService"/>),
/// the ping the client sends on its own, and the options of the options window, which belong to the account.
/// </summary>
public sealed class SystemService(
    World world,
    TimeProvider time,
    IDbContextFactory<PersistenceContext> factory,
    CommunityNetwork community
) :
    IHotspotHandler<SystemOptionUpdateRequest>,
    IHotspotHandler<SystemXignCodeRequest>,
    IHotspotHandler<SystemKeepAliveRequest>,
    IHotspotHandler<SystemPingRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemKeepAliveRequest request)
    {
        var component = world.Get<KeepAliveComponent>(context.Player);

        world.Set(context.Player, component.Answer(time.GetTimestamp(), request.TickCount));

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The options window of the game sends every option at once. They are the account's, as at the gate, so the next login
    /// shows them, and the person's copy changes at once: who may whisper to it or see its information is read from it.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, SystemOptionUpdateRequest request)
    {
        var account = world.Get<ClaimsComponent>(context.Player).Account;

        if (account == 0)
        {
            return;
        }

        var options = request.OptionList.ToArray();

        world.Set(context.Player, options.ToPersonOptionComponent());

        await factory.SavePersonOptionsAsync(account, options, context.CancellationToken);

        if (world.IsAlive(context.Player))
        {
            await community.RefreshAsync(context.Player);
        }
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemPingRequest request)
    {
        // A ping comes from the same client as a keep-alive, so it shows the client is still there as well.
        if (world.TryGet(context.Player, out KeepAliveComponent keepAlive))
        {
            world.Set(context.Player, keepAlive.Heard(time.GetTimestamp()));
        }

        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SystemPingResponse { KeepAlive = request.TickCount });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SystemXignCodeRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
