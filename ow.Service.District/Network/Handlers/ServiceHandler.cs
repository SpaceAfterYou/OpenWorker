using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using ow.Framework.Utils;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using System.Linq;

namespace ow.Service.District.Network.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(ServerOpcode.DistrictEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, DistrictEnterRequest request, Instance instance, DayEventBoosterRepository dayEventBoosters, DimensionRepository dimensions, LanContext lan)
        {
            if (request.Account != lan.GetAccountIdBySessionKey(request.SessionKey))
                NetworkUtils.DropSession();

            {
                AccountModel model = GetAccountModel(request.Account);
                session.Account = new(model);
            }

            {
                CharacterModel model = GetCharacterModel(request.Character, request.Account);

                session.Character = new(model);
                session.Profile = new(model.Profile);
                session.Stats = new();
                session.SpecialOptions = new();
                session.Gestures = model.Gestures;
            }

            if (!dimensions.Join(session))
            {
                session.Disconnect();
                return;
            }

            session
                .SendAsync(new ServiceCurrentDataResponse())
                .SendAsync(new DayEventBoosterResponse()
                {
                    Values = dayEventBoosters.Select(s => new DayEventBoosterResponse.Entity()
                    {
                        Id = s.Id,
                        Maze = s.Maze.Id
                    }).ToArray()
                })
                .SendAsync(new WorldVersionResponse())
                .SendAsync(new DistrictEnterResponse()
                {
                    Place = new()
                    {
                        Location = instance.Location.Id,
                        Position = session.Character.Place.Position,
                        Rotation = session.Character.Place.Rotation,
                    }
                })
                //.SenBoosterAdd(boosters)
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

        internal static AccountModel GetAccountModel(int id)
        {
            using AccountContext context = new();
            return context.Accounts.First(c => c.Id == id);
        }

        internal static CharacterModel GetCharacterModel(int id, int account)
        {
            using CharacterContext context = new();
            return context.Characters.First(c => c.Id == id && c.AccountId == account);
        }

        [Handler(ServerOpcode.Heartbeat, HandlerPermission.Authorized)]
        public static void Heartbeat(Session session, ServiceHeartbeatRequest request) =>
            session.SendAsync(request);

        [Handler(ServerOpcode.DistrictLogOut, HandlerPermission.Authorized)]
        public static void LogOut(Session session, DistrictLogoutRequest request, GateInstance gate)
        {
            if (session.Account.Id != request.Account)
                NetworkUtils.DropSession();

            if (session.Character.Id != request.Character)
                NetworkUtils.DropSession();

            if (request.Way != DistrictLogOutWay.GoToGateService)
                NetworkUtils.DropSession();

            session.SendAsync(new DistrictLogOutResponse()
            {
                Account = session.Account.Id,
                Character = session.Character.Id,
                Ip = gate.Ip,
                Port = gate.Port,
            });
        }
    }
}