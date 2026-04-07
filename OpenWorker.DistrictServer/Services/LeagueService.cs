using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Types;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Hotspot.Modules.League.Requests;
using OpenWorker.Hotspot.Modules.League.Responses;

namespace OpenWorker.DistrictServer.Services;

public sealed class LeagueService(World world, ServiceChannels channels) :
    IHotspotHandler<LeagueCreateRequest>,
    IHotspotHandler<LeagueOverlapNameRequest>,
    IHotspotHandler<LeagueSearchRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, LeagueOverlapNameRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new LeagueOverlapNameResponse { CanBeUsed = false });
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, LeagueCreateRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var actor = world.Get<ActorComponent>(context.Player);
        var person = context.Player.Get<PersonInfoComponent>();
        var location = context.Player.Get<WorldComponent>();

        var channel = channels.Get(context.Player);

        session.Send(new LeagueCreateResponse
        {
            LeagueInfo = new LeagueInfo
            {
                Identifier = 1,
                Name = request.Name,
                Master = actor.Identifier,
                MasterName = person.Name,
                MemberCount = 1
            },
            Member = new LeagueMember
            {
                Hero = person.Hero,
                Name = person.Name,
                Level = 1,
                Login = true,
                World = location.Location,
                Channel = (byte)channel.Identifier,
                Person = actor.Identifier,
                LeagueInfo = new LeagueInfoForMember
                {
                    League = 1,
                    Position = LeaguePosition.LeagueMaster
                }
            }
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, LeagueListRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new LeagueListResponse
        {
            Leagues = Enumerable.Range(0, 10).Select((_, index) => new LeagueInfo
            {
                Identifier = 1 + index,
                Rank = 1 + index,
                GroupType = request.Type,
                Rating = (byte)(1),
                MemberCount = (short)(10 + index),
                Exp = (1000 + index),
                Name = "League " + index,
                Money = (10000 + index),
                // CreateDate
                // NoticeDate
                Master = (int)(512 + index),
                MasterName = "Master " + index,
                ViceMasterName = "Vice " + index,
                Open = true
            }).ToArray(),

            Actors = Enumerable
                .Range(0, 10)
                .Select((_, index) => new ActorValue((int)(1024 + index), ActorType.User))
                .ToArray()
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, LeagueSearchRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new LeagueSearchResponse
        {
            State = 0,
            Name = string.Empty,
            Master = string.Empty,
            Leagues = Enumerable.Range(0, 10).Select((_, index) => new LeagueInfo
            {
                Identifier = 1 + index,
                Rank = 1 + index,
                // GroupType = 1 + index,
                Rating = (byte)(1 + index),
                MemberCount = (short)(100 + index),
                Exp = (1000 + index),
                Name = "Search L " + index,
                Money = (10000 + index),
                // CreateDate
                // NoticeDate
                Master = (int)(512 + index),
                MasterName = "Search M " + index,
                ViceMasterName = "Search V " + index,
                Open = true
            }).ToArray(),

            Actors = Enumerable
                .Range(0, 10)
                .Select((_, index) => new ActorValue((int)(1024 + index), ActorType.User))
                .ToArray()
        });

        return ValueTask.CompletedTask;
    }
}
