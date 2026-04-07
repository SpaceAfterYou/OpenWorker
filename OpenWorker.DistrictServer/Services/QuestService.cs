using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Quests.Requests;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class QuestService(QuestManager questManager) :
    IHotspotHandler<QuestAcceptRequest>,
    IHotspotHandler<QuestEpisodeCompleteRequest>,
    IHotspotHandler<QuestGiveUpRequest>,
    IHotspotHandler<QuestEventUpdateRequest>,
    IHotspotHandler<QuestHelperRequest>,
    IHotspotHandler<QuestFailRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestAcceptRequest request)
    {
        questManager.AcceptEpisode(context.Player, request.Episode);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestEpisodeCompleteRequest request)
    {
        questManager.CompleteEpisode(context.Player, request.Episode);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestEventUpdateRequest request)
    {
        questManager.AckEventUpdate(context.Player);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestFailRequest request)
    {
        questManager.FailEpisode(context.Player, request.Episode);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestGiveUpRequest request)
    {
        questManager.GiveUpEpisode(context.Player, request.Episode);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestHelperRequest request)
    {
        questManager.SetHelper(context.Player, request.Type, request.Episode);
        return ValueTask.CompletedTask;
    }
}
