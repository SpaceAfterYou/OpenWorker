using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Party;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Requests;
using OpenWorker.Hotspot.Modules.Party.Responses;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.DistrictServer.Services;

public sealed class PartyService(Arch.Core.World world) :
    IHotspotHandler<PartyAcceptRequest>,
    IHotspotHandler<PartyAddMemberRequest>,
    IHotspotHandler<PartyAwaiterAddRequest>,
    IHotspotHandler<PartyAwaiterDelRequest>,
    IHotspotHandler<PartyAwaiterInfoRequest>,
    IHotspotHandler<PartyAwaiterListRequest>,
    IHotspotHandler<PartyCancelRequest>,
    IHotspotHandler<PartyChangeMasterRequest>,
    IHotspotHandler<PartyInviteRequest>,
    IHotspotHandler<PartyKickOutRequest>,
    IHotspotHandler<PartyLeaveRequest>,
    IHotspotHandler<PartyMatchingCheckRequest>,
    IHotspotHandler<PartyMatchingEnterRequest>,
    IHotspotHandler<PartyMatchingExitRequest>,
    IHotspotHandler<PartyMatchingResetRequest>,
    IHotspotHandler<PartyMatchingWaitRequest>,
    IHotspotHandler<PartyMazeClearRequest>,
    IHotspotHandler<PartyRecruitAddRequest>,
    IHotspotHandler<PartyRecruitApplyAcceptRequest>,
    IHotspotHandler<PartyRecruitApplyInfoRequest>,
    IHotspotHandler<PartyRecruitApplyListRequest>,
    IHotspotHandler<PartyRecruitApplyNoticeRequest>,
    IHotspotHandler<PartyRecruitApplyRejectRequest>,
    IHotspotHandler<PartyRecruitApplyRequest>,
    IHotspotHandler<PartyRecruitApplyUpdateRequest>,
    IHotspotHandler<PartyRecruitDelRequest>,
    IHotspotHandler<PartyRecruitInfoRequest>,
    IHotspotHandler<PartyRecruitListRequest>,
    IHotspotHandler<PartyRecruitPenaltyRequest>,
    IHotspotHandler<PartyUnknown5Request>,
    IHotspotHandler<PartyUpdateInfoRequest>,
    IHotspotHandler<PartyUpdateMemberHpRequest>,
    IHotspotHandler<PartyUpdateMemberInfoRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyAcceptRequest request)
    {
        GetSession(context).Send(CreateAcceptResponse(1, 0));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyAddMemberRequest request)
    {
        GetSession(context).Send(new PartyAddMemberResponse
        {
            Info = CreateMemberInfo("FakeMember", 1001),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterAddRequest request)
    {
        GetSession(context).Send(new PartyAwaiterAddResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterDelRequest request)
    {
        GetSession(context).Send(new PartyAwaiterDelResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterInfoRequest request)
    {
        GetSession(context).Send(new PartyAwaiterInfoResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyAwaiterListRequest request)
    {
        GetSession(context).Send(new PartyAwaiterListResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyCancelRequest request)
    {
        GetSession(context).Send(new PartyCancelResponse
        {
            ReqActor = CreateActor(1000),
            RejActor = CreateActor(1001),
            RejName = "FakeReject",
            ErrorCode = 0,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyChangeMasterRequest request)
    {
        GetSession(context).Send(new PartyChangeMasterResponse
        {
            Master = CreateActor(1000),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyInviteRequest request)
    {
        GetSession(context).Send(CreateInviteResponse(request));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyKickOutRequest request)
    {
        GetSession(context).Send(new PartyDeleteResponse
        {
            Member = CreateMemberInfo("KickedMember", 1002),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyLeaveRequest request)
    {
        GetSession(context).Send(new PartyLeaveResponse
        {
            Identifier = 1,
            Member = CreateActor(1000),
            IsKickOut = false,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingCheckRequest request)
    {
        GetSession(context).Send(new PartyMatchingCheckResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingEnterRequest request)
    {
        GetSession(context).Send(new PartyMatchingEnterResponse
        {
            Identifier = 1,
            MemberList =
            [
                CreateMemberInfo("MatchingLeader", 1003),
                CreateMemberInfo("MatchingDps", 1004),
                CreateMemberInfo("MatchingSupport", 1005),
            ],
            RemainTick = 120,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingExitRequest request)
    {
        GetSession(context).Send(new PartyMatchingExitResponse()
        {
            Character = 1000,
            Reason = default,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingResetRequest request)
    {
        GetSession(context).Send(new PartyMatchingResetResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyMatchingWaitRequest request)
    {
        GetSession(context).Send(new PartyMatchingWaitResponse()
        {
            Member = CreateActor(1004),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyMazeClearRequest request)
    {
        GetSession(context).Send(new PartyMazeClearResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitAddRequest request)
    {
        GetSession(context).Send(new PartyRecruitAddResponse()
        {
            IsSuccess = true,
            Unknown = 60f,
            Member = CreateRecruitMember(),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyAcceptRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyAcceptResponse()
        {
            Actor = CreateActor(1005),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyInfoRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyInfoResponse()
        {
            MemberList =
            [
                CreateMemberInfo("ApplyInfoMember1", 1006),
                CreateMemberInfo("ApplyInfoMember2", 1007),
                CreateMemberInfo("ApplyInfoMember3", 1008),
            ],
            Actor = CreateActor(1006),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyListRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyListResponse()
        {
            ValueList = Enumerable
                .Range(0, PartyModuleDefines.RecruitAppliesCount)
                .Select(e => new PartyApplyMemberValue
                {
                    Member = CreateMemberInfo($"ApplyListMember{e}", 2007 + e),
                    RegDate = 1800
                })
                .ToArray()
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyNoticeRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyNoticeResponse()
        {
            Member = new PartyApplyMemberValue()
            {
                Member = CreateMemberInfo("ApplyNoticeMember", 1008),
                RegDate = 0,
            },
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyRejectRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyRejectResponse()
        {
            Actor = CreateActor(1009),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyResponse()
        {
            ErrorCode = default,
            Type = default,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitApplyUpdateRequest request)
    {
        GetSession(context).Send(new PartyRecruitApplyUpdateResponse()
        {
            Actor = CreateActor(1010),
            Level = 10,
            Location = 100,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitDelRequest request)
    {
        GetSession(context).Send(new PartyRecruitDelResponse()
        {
            Party = 1,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitInfoRequest request)
    {
        GetSession(context).Send(new PartyRecruitInfoResponse()
        {
            Recruit = CreateRecruitMember(),
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitListRequest request)
    {
        GetSession(context).Send(new PartyRecruitListResponse()
        {
            Values =
            [
                CreateRecruitMember(),
                new PartyRecruitMemberValue
                {
                    Party = 2,
                    Message = "Fake recruit 2",
                    MinLevel = 5,
                    MaxLevel = 85,
                    Purpose = 1,
                    MasterName = "FakeMaster2",
                    MemberCount = 2,
                    RemainTime = 2400,
                    PurposeLocation = 21311,
                },
                new PartyRecruitMemberValue
                {
                    Party = 3,
                    Message = "Fake recruit 3",
                    MinLevel = 10,
                    MaxLevel = 85,
                    Purpose = 2,
                    MasterName = "FakeMaster3",
                    MemberCount = 3,
                    RemainTime = 1800,
                    PurposeLocation = 21311,
                },
            ],
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyRecruitPenaltyRequest request)
    {
        GetSession(context).Send(new PartyRecruitPenaltyResponse()
        {
            Time = 30f,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyUnknown5Request request)
    {
        GetSession(context).Send(new PartyUnknown5Response());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyUpdateInfoRequest request)
    {
        GetSession(context).Send(new PartyUpdateInfoResponse()
        {
            Party = 1,
            Master = world.Get<ActorComponent>(context.Player),
            Maze = default,
            UpdateType = default,
            Type = default,
            InfoList =
            [
                CreateMemberInfo("UpdatedMember1", 1011),
                CreateMemberInfo("UpdatedMember2", 1012),
                CreateMemberInfo("UpdatedMember3", 1013),
            ]
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyUpdateMemberHpRequest request)
    {
        GetSession(context).Send(new PartyUpdateMemberHpResponse()
        {
            Actor = CreateActor(1012),
            Health = 100,
            MaxHealth = 100,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyUpdateMemberInfoRequest request)
    {
        GetSession(context).Send(new PartyUpdateMemberInfoResponse()
        {
            Party = 1,
            Member = CreateMemberInfo("UpdatedInfoMember", 1013),
        });

        return ValueTask.CompletedTask;
    }

    private ServerSessionComponent GetSession(ServiceHandleContext context) => world.Get<ServerSessionComponent>(context.Player);

    private static PartyAcceptResponse CreateAcceptResponse(int identifier, int result) => new()
        {
            Identifier = identifier,
            Result = result,
        };

    private static PartyInviteResponse CreateInviteResponse(PartyInviteRequest request) => new()
        {
            MasterActor = CreateActor(1000),
            RequestActor = CreateActor(1001),
            MasterName = request.RequesterName,
            RequestName = request.TargetName,
            ReqServer = 0,
            Result = 0,
        };

    private static ActorValue CreateActor(int identifier) => new(identifier, ActorType.User);

    private static PartyMemberInfoValue CreateMemberInfo(string name, int actorId) => new()
        {
            Actor = CreateActor(actorId),
            Name = name,
            Level = 10,
            Class = Hero.Haru,
            Location = 10003,
            Channel = 1,
            MaxHealth = 100,
            Health = 100,
            IsLoggedIn = true,
            Map = new MapValue
            {
                Channel = 1,
                Location = 10003,
                Server = 0
            },
        };

    private static PartyRecruitMemberValue CreateRecruitMember() => new()
        {
            Party = 1,
            Message = "Fake recruit",
            MinLevel = 1,
            MaxLevel = 85,
            Purpose = 0,
            MasterName = "FakeMaster",
            MemberCount = 1,
            RemainTime = 3600,
            PurposeLocation = 10003,
        };
}
