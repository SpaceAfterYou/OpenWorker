using ow.Framework.Game.Character;
using ow.Framework.Game.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Server;
using ow.Framework.Utils;
using ow.Service.District.Game;

namespace ow.Service.District.Network.Handlers
{
    internal static class ServiceHandler
    {
        //public Channel Channel { get; set; }
        //public Character Character { get; init; }
        //public Profile Profile { get; init; }

        [Handler(ServerOpcode.DistrictEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(GameSession session, EnterRequest request, IBoosterRepository boosters, IDayEventBoosterRepository dayEventBoosterRepository, IChannelRepository channels)
        {
            var accountModel = await DistrictEnterHelper.GetAccountModel(request.AccountId, request.SessionKey);
            session.SetComponent(new Account(accountModel));

            var characterModel = await DistrictEnterHelper.GetCharacterModel(request.CharacterId, request.AccountId);

            var character = new Character(characterModel);
            session
                .SetComponent(character)
                .SetComponent(new ProfileEntity(characterModel))
                .SetComponent(new StatsEntity())
                .SetComponent(new SpecialOptionsEntity())
                .SetComponent(new GesturesEntity(characterModel));

            DistrictEnterHelper.CreateStorages(session, accountModel, characterModel);

            //Hub.SendSessionConnect(session, character);

            channels.JoinToFirstAvailable(session);

            session
                .ReTarget<AuthorizedGroupAttribute>()
                .SendCurrentDate()
                .SendWorldVersion()
                .SendDayEventBoosterList(dayEventBoosterRepository)
                .SendWorldEnter()
                .SendAddBoosters(boosters)
                //eSUB_CMD_POST_ACCOUNT_RECV
                //.SendAttendanceRewardLoad()
                //.SendAttendancePlayTimeInit()
                //receive_eSUB_CMD_EXCHANGE_INTEREST_LIST
                //receive_eSUB_CMD_EVENT_ROULETTE_MY_INFO
                //receive_eSUB_CMD_ITEM_AKASHIC_GETINFO_LOAD
                //.SendInfiniteTowerLoadInfo()
                //receive_eSUB_CMD_ENTER_MAZE_LIMIT_COUNT_RESET
                //.SendAttendanceReward()
                //.SendAttendanceContinueReward()
                // receive_eSUB_CMD_FRIEND_LOAD
                // receive_eSUB_CMD_FRIEND_LOAD_BLOCKLIST
                .SendCharacterDbLoadSync();
        }

        [Handler(ServerOpcode.Heartbeat, HandlerPermission.Authorized)]
        public static void Heartbeat(GameSession session, HeartbeatRequest request) =>
            session.SendServerHeartbeat(request);

        [Handler(ServerOpcode.DistrictLogOut, HandlerPermission.Authorized)]
        public static void LogOut(GameSession session, LogoutRequest request, GateInstance gate)
        {
            if (!session.Entity.Has<AccountEntity>())
                NetworkUtils.DropSession();

            if (session.Entity.Get<AccountEntity>().Id != request.AccountId)
                NetworkUtils.DropSession();

            if (!session.Entity.Has<EntityCharacter>())
                NetworkUtils.DropSession();

            if (session.Entity.Get<EntityCharacter>().Id != request.CharacterId)
                NetworkUtils.DropSession();

            session.SendServerLogOut(gate);
        }
    }
}