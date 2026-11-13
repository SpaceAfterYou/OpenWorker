using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The rewards of the window of the achievements, in a district and in a maze: the reward of an achievement and the reward
/// of the share points of a district or a group of mazes.
/// </summary>
public sealed class ProgressService(World world, AchievementManager achievements, SharePointManager sharePoints, TitleBook titles) :
    IHotspotHandler<PersonAchieveRewardRequest>,
    IHotspotHandler<PersonGetRewardSharePointRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, PersonAchieveRewardRequest request)
    {
        var reward = achievements.TakeReward(context.Player, request.Index);

        if (reward.Error != 0)
        {
            Send(context.Player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.AchieveReward), reward.Error));
            return;
        }

        if (!reward.Given)
        {
            return;
        }

        // The reward is taken before anything is waited for, so it cannot be taken twice.
        Send(context.Player, new PersonAchieveRewardResponse { Index = request.Index });

        foreach (var title in reward.Titles)
        {
            if (!world.IsAlive(context.Player))
            {
                return;
            }

            await titles.GrantAsync(context.Player, title, TitleSource.Achievement);
        }
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PersonGetRewardSharePointRequest request)
    {
        sharePoints.TakeReward(context.Player, request.Type, request.Identifier);

        return ValueTask.CompletedTask;
    }

    private void Send<T>(Entity player, in T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(in message);
        }
    }
}
