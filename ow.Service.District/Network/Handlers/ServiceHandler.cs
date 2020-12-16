using ow.Framework.Game.Character;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Server;
using ow.Service.District.Game;
using System.Diagnostics;

namespace ow.Service.District.Network.Handlers
{
    internal static class ServiceHandler
    {
        //public Channel Channel { get; set; }
        //public Character Character { get; init; }
        //public Profile Profile { get; init; }

        //[Handler(ServerOpcode.DistrictEnter, HandlerPermission.UnAuthorized)]
        //public static void Enter(Session session, EnterRequest request, IBoosterRepository boosters, IDayEventBoosterRepository dayEventBoosterRepository, IChannelRepository channels)
        //{
        //    var accountModel = await DistrictEnterHelper.GetAccountModel(request.AccountId, request.SessionKey);
        //    session.SetComponent(new Account(accountModel));

        //    var characterModel = await DistrictEnterHelper.GetCharacterModel(request.CharacterId, request.AccountId);

        //    var character = new Character(characterModel);
        //    session
        //        .SetComponent(character)
        //            .SetComponent(new Profile(characterModel))
        //            .SetComponent(new Stats())
        //            .SetComponent(new SpecialOptions())
        //            .SetComponent(new Gestures(characterModel));

        //    DistrictEnterHelper.CreateStorages(session, accountModel, characterModel);

        //    //Hub.SendSessionConnect(session, character);

        //    channels.JoinToFirstAvailable(session);

        //    session
        //        .ReTarget<AuthorizedGroupAttribute>()
        //        .SendCurrentDate()
        //        .SendWorldVersion()
        //        .SendDayEventBoosterList(dayEventBoosterRepository)
        //        .SendWorldEnter()
        //        .SendAddBoosters(boosters)
        //        //eSUB_CMD_POST_ACCOUNT_RECV
        //        //.SendAttendanceRewardLoad()
        //        //.SendAttendancePlayTimeInit()
        //        //receive_eSUB_CMD_EXCHANGE_INTEREST_LIST
        //        //receive_eSUB_CMD_EVENT_ROULETTE_MY_INFO
        //        //receive_eSUB_CMD_ITEM_AKASHIC_GETINFO_LOAD
        //        //.SendInfiniteTowerLoadInfo()
        //        //receive_eSUB_CMD_ENTER_MAZE_LIMIT_COUNT_RESET
        //        //.SendAttendanceReward()
        //        //.SendAttendanceContinueReward()
        //        // receive_eSUB_CMD_FRIEND_LOAD
        //        // receive_eSUB_CMD_FRIEND_LOAD_BLOCKLIST
        //        .SendCharacterDbLoadSync();
        //}

        //[Handler(ServerOpcode.Heartbeat, HandlerPermission.Authorized)]
        //public static void Heartbeat(Session session, HeartbeatRequest request) =>
        //session.SendServerHeartbeat(request);

        [Handler(ServerOpcode.LogOut, HandlerPermission.Authorized)]
        public static void LogOut(GameSession session, LogoutRequest request, GateInstance gate)
        {
            if (!session.Entity.Has<AccountEntity>())
#if !DEBUG
                throw new BadActionException();
#else
                Debug.Assert(false);
#endif

            if (session.Entity.Get<AccountEntity>().Id != request.AccountId)

#if !DEBUG
                throw new BadActionException();
#else
                Debug.Assert(false);
#endif

            if (!session.Entity.Has<EntityCharacter>())
#if !DEBUG
                throw new BadActionException();
#else
                Debug.Assert(false);
#endif

            if (session.Entity.Get<EntityCharacter>().Id != request.CharacterId)

#if !DEBUG
                throw new BadActionException();
#else
                Debug.Assert(false);
#endif

            session.SendServerLogOut(gate);
        }
    }
}