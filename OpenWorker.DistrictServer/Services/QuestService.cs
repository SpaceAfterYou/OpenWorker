using Arch.Core;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Quests.Requests;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class QuestService :
    IHotspotHandler<QuestAcceptRequest>,
    IHotspotHandler<QuestEpisodeCompleteRequest>,
    IHotspotHandler<QuestGiveUpRequest>,
    IHotspotHandler<QuestEventUpdateRequest>,
    IHotspotHandler<QuestHelperRequest>,
    IHotspotHandler<QuestFailRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestAcceptRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestEpisodeCompleteRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestEventUpdateRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestFailRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestGiveUpRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, QuestHelperRequest request)
    {
        return ValueTask.CompletedTask;
    }
}