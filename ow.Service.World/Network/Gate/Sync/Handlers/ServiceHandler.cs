using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Relay.Global;
using ow.Framework.IO.Network.Relay.Global.Protos.Requests;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.Utils;
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
                NetworkUtils.DropSession();

            if (!_relay.Session.Validate(new RGSSessionValidateRequest { Account = request.Account, Key = request.SessionKey }).Result)
                NetworkUtils.DropSession();

            {
                using AccountContext context = _accountFactory.CreateDbContext();

                AccountModel model = context.Accounts.AsNoTracking().First(c => c.Id == request.Account);

                session.Account = new(model);
                session.Characters = GetCharacters(model, request.Gate);
                session.Background = model.CharacterBackground;
            }

            session.SendAsync(new GateEnterResponse() { AccountId = request.Account, Result = GateEnterResult.Success });
            session.SendAsync(new ServiceCurrentDataResponse());
        }

        private Characters GetCharacters(AccountModel model, ushort gate)
        {
            using CharacterContext characterContext = _characterFactory.CreateDbContext();
            using ItemContext itemContext = _itemFactory.CreateDbContext();

            return new(model, gate, _tables, characterContext, itemContext);
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