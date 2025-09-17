using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Ranking.Requests;
using OpenWorker.Hotspot.Modules.Ranking.Responses;
using OpenWorker.Hotspot.Modules.Ranking.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class RankingService(World world) :
    IHotspotHandler<RankingTimeAttackListRequest>,
    IHotspotHandler<RankingPvpListRequest>,
    IHotspotHandler<RankingInfiniteTowerListRequest>    
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, RankingTimeAttackListRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(RankingTimeAttackListResponse.Create(world, context.Player, request.World));
        
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, RankingPvpListRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new RankingPvpListResponse(world, context.Player, RankingType.Weekly));
        session.Send(new RankingPvpListResponse(world, context.Player, RankingType.Seasonal));
        
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, RankingInfiniteTowerListRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(RankingInfiniteTowerListResponse.Create(request.Chapter));
        
        return ValueTask.CompletedTask;
    }
}