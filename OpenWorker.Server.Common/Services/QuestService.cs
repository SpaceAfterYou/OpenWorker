using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Quests.Requests;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The requests about quests that the client sends in a maze: the same as in a district.
/// </summary>
public sealed class QuestService(QuestManager questManager) :
    IHotspotHandler<QuestAcceptRequest>,
    IHotspotHandler<QuestEpisodeCompleteRequest>,
    IHotspotHandler<QuestGiveUpRequest>,
    IHotspotHandler<QuestEventUpdateRequest>,
    IHotspotHandler<QuestHelperRequest>,
    IHotspotHandler<QuestFailRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, QuestAcceptRequest request)
    {
        await questManager.AcceptAsync(context.Player, request.Episode, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, QuestEpisodeCompleteRequest request)
    {
        await questManager.CompleteAsync(context.Player, request.Episode, request.RewardItem, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, QuestEventUpdateRequest request)
    {
        await questManager.OnEventUpdateAsync(context.Player, request.Condition, request.Param, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, QuestFailRequest request)
    {
        await questManager.RemoveFailedAsync(context.Player, request.Episode);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, QuestGiveUpRequest request)
    {
        await questManager.GiveUpAsync(context.Player, request.Episode, context.CancellationToken);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, QuestHelperRequest request)
    {
        await questManager.SetHelperAsync(context.Player, request.Type, request.Episode);
    }
}
