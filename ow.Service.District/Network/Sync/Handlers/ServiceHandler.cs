using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.CS;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.Utils;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using System.Linq;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class ServiceHandler
    {
        [Handler(ServerOpcode.DistrictEnter, HandlerPermission.UnAuthorized)]
        public void Enter(Session session, DistrictEnterRequest request)
        {
            if (request.Account != _lan.GetSessionKey(request.SessionKey))
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

                using ItemContext context = _itemFactory.CreateDbContext();
                session.Storages = new(model, _tables, context);
            }

            if (!_dimensions.Join(session))
                NetworkUtils.DropSession();

            session
                .SendAsync(new ServiceCurrentDataResponse())
                .SendAsync(new DayEventBoosterResponse()
                {
                    Values = _dayEventBoosters.Select(s => new DayEventBoosterResponse.Entity()
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
                        Location = _instance.Location.Id,
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

        private AccountModel GetAccountModel(int id)
        {
            using AccountContext context = _accountFactory.CreateDbContext();
            return context.Accounts.First(c => c.Id == id);
        }

        private CharacterModel GetCharacterModel(int id, int account)
        {
            using CharacterContext context = _characterFactory.CreateDbContext();
            return context.Characters.First(c => c.Id == id && c.AccountId == account);
        }

        [Handler(ServerOpcode.Heartbeat, HandlerPermission.Authorized)]
        public static void Heartbeat(Session session, ServiceHeartbeatRequest request) =>
            session.SendAsync(request);

        [Handler(ServerOpcode.DistrictLogOut, HandlerPermission.Authorized)]
        public void LogOut(Session session, DistrictLogoutRequest request)
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
                Ip = _gate.Ip,
                Port = _gate.Port,
            });
        }

        public ServiceHandler(IDbContextFactory<ItemContext> itemFactory, IDbContextFactory<AccountContext> accountFactory, IDbContextFactory<CharacterContext> characterFactory, Instance instance, DayEventBoosterRepository dayEventBoosters, DimensionRepository dimensions, CSClient lan, BinTables tables, GateInstance gate)
        {
            _itemFactory = itemFactory;
            _accountFactory = accountFactory;
            _characterFactory = characterFactory;
            _instance = instance;
            _dayEventBoosters = dayEventBoosters;
            _dimensions = dimensions;
            _lan = lan;
            _tables = tables;
            _gate = gate;
        }

        private readonly IDbContextFactory<ItemContext> _itemFactory;
        private readonly IDbContextFactory<AccountContext> _accountFactory;
        private readonly IDbContextFactory<CharacterContext> _characterFactory;
        private readonly Instance _instance;
        private readonly DayEventBoosterRepository _dayEventBoosters;
        private readonly DimensionRepository _dimensions;
        private readonly CSClient _lan;
        private readonly BinTables _tables;
        private readonly GateInstance _gate;
    }
}