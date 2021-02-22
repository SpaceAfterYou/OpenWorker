using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.IO.Network.Relay.Global;
using ow.Framework.IO.Network.Relay.Global.Protos.Responses;
using ow.Framework.Utils;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using SoulCore.Data.Bin.Table.Entities;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Permissions;
using SoulCore.IO.Network.Requests;
using SoulCore.IO.Network.Requests.Character;
using SoulCore.IO.Network.Responses;
using SoulCore.Types;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ow.Service.District.Network.Sync.Handlers
{
    public sealed class ServiceHandler
    {
        private readonly IDbContextFactory<CharacterContext> _characterFactory;
        private readonly IDbContextFactory<AccountContext> _accountFactory;
        private readonly IDbContextFactory<ItemContext> _itemFactory;
        private readonly DayEventBoosterRepository _dayEventBoosters;
        private readonly ChannelRepository _channels;
        private readonly RGClient _globalRelay;
        private readonly GateInstance _gate;
        private readonly Instance _instance;
        private readonly BinTable _tables;

        [Handler(CategoryCommand.Character, CharacterCommand.EnterGameServerReq, HandlerPermission.Anonymous)]
        public void Enter(SyncSession session, CharacterEnterGameServerRequest request)
        {
            _globalRelay.Session.ContainsAsync(new() { Account = request.Account, Key = request.SessionKey }).ResponseAsync.ContinueWith((Task<RGSSessionContainsResponse> result) =>
            {
                if (!result.Result.Result)
                {
                    NetworkUtils.DropBadAction();
                }

                session.Account = new(GetAccountModel(request.Account));

                {
                    CharacterModel model = GetCharacterModel(request.Character, request.Account);

                    session.Character = new(model);
                    session.Profile = new(model.Profile);
                    session.Gestures = model.Gestures;

                    using ItemContext context = _itemFactory.CreateDbContext();
                    session.Storages = new(model, _tables, context);
                }

                if (!_channels.TryJoin(session))
                {
                    NetworkUtils.DropBadAction();
                }

                if (!session.Server.Characters.TryAdd(session.Character.Id, session))
                {
                    NetworkUtils.DropBadAction();
                }

                session.Permission = HandlerPermission.Authorized;

                session.SendAsync(new SWorldCurrentDataResponse());
                session.SendAsync(new WorldVersionResponse()
                {
                    Id = 0,
                    Main = 1,
                    Sub = 837,
                    Data = 16888
                });

                if (_tables.BattlePass.Count > 0)
                {
                    PassInfoEntity entity = _tables.BattlePass.Values.First();
                    session.SendAsync(new BattlePassLoadResponse()
                    {
                        Id = entity.Id,
                        HavePoint = 2000,
                        NextReward = 3,
                        StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        EndDate = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds(),
                        IsPremium = 1
                    });
                }

                session.SendAsync(new DayEventBoosterResponse()
                {
                    Values = _dayEventBoosters.Select(s => new DayEventBoosterResponse.Entity()
                    {
                        Id = s.Id,
                        Maze = s.Maze.Id
                    }).ToArray()
                });

                // CharacterSuperArmorGage
                // LOGLV 2 : 454.747 :: eSUB_CMD_BOOSTER_ADD 2

                session.SendAsync(new DistrictEnterResponse()
                {
                    Place = new()
                    {
                        Location = _instance.LocationId,
                        Position = session.Character.Place.Position,
                        Rotation = session.Character.Place.Yaw,
                    }
                });

                //LOGLV 2 : 454.771 :: eSUB_CMD_POST_ACCOUNT_RECV

                //LOGLV 2 : 454.771 :: receive_eSUB_CMD_EVENT_BATTLE_PASS_LOAD

                //LOGLV 2 : 454.771 :: receive_eSUB_CMD_EVENT_ATTENDANCE_LOAD
                // CharacterStatsUpdate
                //LOGLV 2 : 454.806 :: receive_eSUB_CMD_EXCHANGE_INTEREST_LIST
                //LOGLV 2 : 454.806 :: receive_eSUB_CMD_EVENT_ROULETTE_MY_INFO
                //LOGLV 2 : 454.806 :: eSUB_CMD_INFINITE_TOWER_LOAD_INFO
                //LOGLV 2 : 454.806 :: receive_eSUB_CMD_ENTER_MAZE_LIMIT_COUNT_RESET
                //LOGLV 2 : 454.806 :: receive_eSUB_CMD_EVENT_ATTENDANCE_PLAY_TIME_INIT
                //LOGLV 2 : 454.830 :: receive_eSUB_CMD_ITEM_AKASHIC_GETINFO_LOAD
                //LOGLV 2 : 454.830 :: receive_eSUB_CMD_EVENT_ATTENDANCE_REWARD
                //LOGLV 2 : 454.830 :: receive_eSUB_CMD_EVENT_ATTENDANCE_CONTINUE_REWARD
                //LOGLV 2 : 454.830 :: send_eSUB_CMD_CHARACTER_INFO_REQ
                //LOGLV 2 : 454.830 :: receive_eSUB_CMD_CHARACTER_DB_LOAD_SYNC
                //LOGLV 2 : 454.863 :: eSUB_CMD_POST_ACCOUNT_RECV
                //LOGLV 2 : 454.863 :: eSUB_CMD_POST_ACCOUNT_RECV
                //LOGLV 2 : 455.905 :: receive_eSUB_CMD_CHARACTER_INFO_RES

                session.SendCharacterDbLoadDeferred();
            });
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

        [Handler(ServerOpcode.Heartbeat)]
        public static void Heartbeat(SyncSession session, ServiceHeartbeatRequest request) =>
            session.SendAsync(request);

        [Handler(ServerOpcode.DistrictLogOut)]
        public void LogOut(SyncSession session, DistrictLogoutRequest request)
        {
            if (session.Account.Id != request.Account)
            {
                NetworkUtils.DropBadAction();
            }

            if (session.Character.Id != request.Character)
            {
                NetworkUtils.DropBadAction();
            }

            if (request.Way != DistrictLogOutWay.GoToGateService)
            {
                NetworkUtils.DropBadAction();
            }

            session.SendAsync(new DistrictLogOutResponse
            {
                Account = session.Account.Id,
                Character = session.Character.Id,
                Ip = _gate.Ip,
                Port = _gate.Port,
            });
        }

        public ServiceHandler(IDbContextFactory<ItemContext> itemFactory, IDbContextFactory<AccountContext> accountFactory, IDbContextFactory<CharacterContext> characterFactory, Instance instance, DayEventBoosterRepository dayEventBoosters, ChannelRepository channel, RGClient globalRelay, BinTable tables, GateInstance gate)
        {
            _itemFactory = itemFactory;
            _accountFactory = accountFactory;
            _characterFactory = characterFactory;
            _instance = instance;
            _dayEventBoosters = dayEventBoosters;
            _channels = channel;
            _globalRelay = globalRelay;
            _tables = tables;
            _gate = gate;
        }
    }
}
