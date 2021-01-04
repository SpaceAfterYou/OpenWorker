using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.Utils;
using ow.Service.Gate.Game;
using ow.Service.Gate.Network.Relay;
using System.Linq;

namespace ow.Service.Gate.Network.Handlers
{
    public sealed class ServiceHandler
    {
        [Handler(ServerOpcode.GateEnter, HandlerPermission.UnAuthorized)]
        public void Enter(Session session, GateEnterRequest request)
        {
            if (_gate.Id != request.Gate)
                NetworkUtils.DropSession();

            if (!_relayClient.Session.Validate(new() { Account = request.Account, Key = request.SessionKey }).Result)
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

        public ServiceHandler(GateInstance gate, RelayClient relayClient, BinTables tables, IDbContextFactory<ItemContext> itemFactory, IDbContextFactory<AccountContext> accountFactory, IDbContextFactory<CharacterContext> characterFactory)
        {
            _gate = gate;
            _relayClient = relayClient;
            _tables = tables;
            _itemFactory = itemFactory;
            _accountFactory = accountFactory;
            _characterFactory = characterFactory;
        }

        private readonly GateInstance _gate;
        private readonly RelayClient _relayClient;
        private readonly BinTables _tables;
        private readonly IDbContextFactory<ItemContext> _itemFactory;
        private readonly IDbContextFactory<AccountContext> _accountFactory;
        private readonly IDbContextFactory<CharacterContext> _characterFactory;
    }
}