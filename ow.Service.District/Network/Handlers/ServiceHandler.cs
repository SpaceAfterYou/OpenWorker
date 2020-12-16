using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Entities;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Server;
using ow.Framework.Utils;
using ow.Service.District.Game;
using ow.Service.District.Game.Entities;
using System.Linq;

namespace ow.Service.District.Network.Handlers
{
    internal static class ServiceHandler
    {
        //public Channel Channel { get; set; }
        //public Character Character { get; init; }
        //public Profile Profile { get; init; }

        [Handler(ServerOpcode.DistrictEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(GameSession session, EnterRequest request, IBoosterRepository boosters, IDayEventBoosterRepository dayEventBoosterRepository, IChannelRepository channels, LanContext lan, IBinTables tables)
        {
            if (request.AccountId != lan.GetAccountIdBySessionKey(request.SessionKey))
                NetworkUtils.DropSession();

            AccountModel accountModel = GetAccountModel(request.AccountId);
            session.Entity.Set(new AccountEntity(accountModel));

            CharacterModel characterModel = GetCharacterModel(request.CharacterId, request.AccountId);

            session.Entity.Set(new EntityCharacter(characterModel, tables));
            session.Entity.Set(new ProfileEntity(characterModel.Profile));
            session.Entity.Set(new StatsEntity());
            session.Entity.Set(new SpecialOptionsEntity());
            session.Entity.Set(new GesturesEntity(characterModel));

            //DistrictEnterHelper.CreateStorages(session, accountModel, characterModel);

            //Hub.SendSessionConnect(session, character);

            channels.JoinToFirstAvailable(session);

            session
                .SendServiceCurrentDate()
                .SendServiceWorldVersion()
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

        public static AccountModel GetAccountModel(int id)
        {
            using AccountContext context = new();
            return context.Accounts.First(c => c.Id == id);
        }

        public static CharacterModel GetCharacterModel(int id, int account)
        {
            using CharacterContext context = new();
            return context.Characters.First(c => c.Id == id && c.AccountId == account);
        }

        [Handler(ServerOpcode.Heartbeat, HandlerPermission.Authorized)]
        public static void Heartbeat(GameSession session, HeartbeatRequest request) =>
            session.SendServiceHeartbeat(request);

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

            session.SendServiceLogOut(gate);
        }
    }
}