using Microsoft.EntityFrameworkCore;
using SoulCore.Database.Accounts;
using SoulCore.Database.Characters;
using SoulCore.Database.Storages;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Relay.Global;
using SoulCore.IO.Network.Relay.Global.Protos.Requests;
using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using SoulCore.Utils;
using ow.Service.World.Game.Gate;
using System.Linq;

namespace ow.Service.World.Network.Gate.Sync.Handlers
{
    public sealed class ServiceHandler
    {
        [Handler(ServerOpcode.GateEnter, HandlerPermission.Anonymous)]
        public void Enter(SyncSession session, GateEnterRequest request)
        {
            if (_gate.Id != request.Gate)
                NetworkUtils.DropBadAction();

            if (!_relay.Session.Contains(new RGSSessionContainsRequest { Account = request.Account, Key = request.SessionKey }).Result)
                NetworkUtils.DropBadAction();

            {
                using AccountContext context = _accountFactory.CreateDbContext();

                AccountModel model = context.Accounts.AsNoTracking().First(c => c.Id == request.Account);

                session.Account = new(model);
                session.Characters = GetCharacters(model, request.Gate);
                session.Background = model.CharacterBackground;
                session.Permission = HandlerPermission.Authorized;
            }

            session.SendDeferred(new GateEnterResponse() { AccountId = request.Account, Result = GateEnterResult.Success });
            session.SendDeferred(new SWorldCurrentDataResponse());
        }

        private Characters GetCharacters(AccountModel model, ushort gate)
        {
            using CharacterContext characterContext = _characterFactory.CreateDbContext();
            using ItemContext itemContext = _itemFactory.CreateDbContext();

            return new(model, gate, characterContext, itemContext);
        }

        public ServiceHandler(Instance gate, RGClient relay, BinTables tables, IDbContextFactory<ItemContext> itemFactory, IDbContextFactory<AccountContext> accountFactory, IDbContextFactory<CharacterContext> characterFactory)
        {
            _gate = gate;
            _relay = relay;
            _tables = tables;
            _itemFactory = itemFactory;
            _accountFactory = accountFactory;
            _characterFactory = characterFactory;
        }

        private readonly Instance _gate;
        private readonly RGClient _relay;
        private readonly BinTables _tables;
        private readonly IDbContextFactory<ItemContext> _itemFactory;
        private readonly IDbContextFactory<AccountContext> _accountFactory;
        private readonly IDbContextFactory<CharacterContext> _characterFactory;
    }
}
