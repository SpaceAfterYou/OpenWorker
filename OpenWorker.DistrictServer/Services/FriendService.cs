using System.Collections.ObjectModel;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Friends.Requests;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Friends.Types;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Gameplay;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

public sealed class FriendManager
{

}

[HotspotHandler(HotspotHandlerType.District)]
public sealed class FriendService(World world, ReadOnlyCollection<DistrictRow> districtList, ILogger<FriendService> logger) :
    IHotspotHandler<FriendLoadRequest>,
    IHotspotHandler<FriendLoadBlocklistRequest>,
    IHotspotHandler<FriendInviteRequest>,
    IHotspotHandler<FriendInviteAcceptRequest>,
    IHotspotHandler<FriendDeleteRequest>,
    IHotspotHandler<FriendBlockAddRequest>,
    IHotspotHandler<FriendBlockDelRequest>,
    IHotspotHandler<FriendInfoRequest>,
    IHotspotHandler<FriendFindRequest>,
    IHotspotHandler<FriendRecruitListRequest>,
    IHotspotHandler<FriendRecruitAddRequest>,
    IHotspotHandler<FriendRecruitDelRequest>,
    IHotspotHandler<FriendRecruitInfoRequest>,
    IHotspotHandler<FriendRecommendListRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendBlockAddRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendBlockDelRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendDeleteRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendFindRequest request)
    {
        var tempFriendList = Enumerable
            .Range(10_000, 20)
            .Select(person => new FriendFindEntry
            {
                Person = person,
                Name = $"find. {person}",
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Channel = (byte)Random.Shared.Next(1, byte.MaxValue),
                World = 10003,
                IsLoggedIn = Random.Shared.Next(0, 2) > 0
            }).ToArray();

        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new FriendFindResponse { Values = tempFriendList });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendInfoRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendInviteAcceptRequest request)
    {
        logger.LogDebug("PersonRequested: {RequestPersonRequested} / PersonTarget: {RequestPersonTarget} / TargetName: {RequestTargetName} / IsAccepted: {RequestIsAccepted}", request.PersonRequested, request.PersonTarget, request.TargetName, request.IsAccepted);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendInviteRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendLoadBlocklistRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        var list = Enumerable
            .Range(10_000, 15)
            .Select(person => new FriendBlockValue(
                ++person,
                $"block. {person}",
                (byte)Random.Shared.Next(1, byte.MaxValue)
            ))
            .ToArray();

        session.Send(new FriendLoadBlocklistResponse { List = list });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendLoadRequest request)
    {
        var types = Enumerable.Range(0, 3).ToArray();
        var states = Enumerable.Range(0, 10).ToArray();

        var districts = districtList
            .Where(x => x.Field14 > 0)
            .ToArray();

        var friend = 0;

        var tempFriendList = (
            from _1 in types
            from _2 in states
            let level = (byte)Random.Shared.Next(1, byte.MaxValue)
            let hero = (byte)Random.Shared.Next(1, 7)
            let channel = (byte)Random.Shared.Next(1, byte.MaxValue)
            let state = GetRandomEnumValue<FriendState>()
            select new FriendValue
            {
                Name = $"load. {hero}",
                Friend = (int)++friend,
                Level = level,
                Class = hero,
                State = state,
                CommunityState = CommunityState.Chair,
                Note = $"st:{(byte)state}|lvl:{level}|ch:{channel}",
                Channel = channel,
                World = districts[Random.Shared.Next(0, districts.Length)].Id,
                FriendPoint = (ulong)Random.Shared.Next(0, 1000),
                Login = Random.Shared.Next(0, 2) > 0,
                LogOut = (ulong)DateTime.UtcNow.Ticks
            }).ToList();

        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new FriendLoadResponse { List = tempFriendList });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecommendListRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        var districts = districtList
            .Where(x => x.Field14 > 0)
            .ToArray();

        var list = Enumerable
            .Range(100_000, 3)
            .Select(person => new FriendRecommendValue
            {
                Name = $"recommend. {person}",
                Actor = new ActorValue((int)person, ActorType.User),
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Hero = (Hero)Random.Shared.Next(1, 7),
                Channel = (byte)Random.Shared.Next(1, byte.MaxValue),
                World = districts[Random.Shared.Next(0, districts.Length)].Id,
                IsLoggedIn = 1
            })
            .ToArray();

        session.Send(new FriendRecommendListResponse
        {
            Actor = world.Get<ActorComponent>(context.Player),
            List = list
        });


        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitAddRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitDelRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitInfoRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, FriendRecruitListRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        var districts = districtList
            .Where(x => x.Field14 > 0)
            .ToArray();

        var list = Enumerable
            .Range(200_000, 16)
            .Select(person => new FriendRecruitValue
            {
                Name = $"recruit. {person}",
                Person = (int)person,
                Level = (byte)Random.Shared.Next(1, byte.MaxValue),
                Hero = (Hero)Random.Shared.Next(1, 7),
                Status = (byte)(person % 4),
                Memo = "memo",
                Channel = (byte)Random.Shared.Next(1, byte.MaxValue),
                World = districts[Random.Shared.Next(0, districts.Length)].Id,
                IsLoggedIn = Random.Shared.Next(0, 2) > 0,
                // tLogOut = -1,
                // tAddTime = -1,
            })
            .ToArray();

        session.Send(new FriendRecruitListResponse { List = list });

        return ValueTask.CompletedTask;
    }

    public static T GetRandomEnumValue<T>()
    {
        var values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(Random.Shared.Next(values.Length))!;
    }
}
