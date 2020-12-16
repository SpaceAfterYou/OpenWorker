using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Gate;
using ow.Framework.Utils;
using ow.Service.Gate.Game;
using ow.Service.Gate.Network.Extensions;
using System.Linq;

namespace ow.Service.Gate.Network.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(ServerOpcode.GateEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(GameSession session, EnterRequest request, GateInstance gate, LanContext lan, BinTables tables)
        {
            if (gate.Id != request.GateId) NetworkUtils.DropSession();

            if (request.AccountId != lan.GetAccountIdBySessionKey(request.SessionKey))
                NetworkUtils.DropSession();

            using AccountContext context = new();

            AccountModel model = context.Accounts.AsNoTracking().FirstOrDefault(c => c.Id == request.AccountId);
            if (model is null) NetworkUtils.DropSession();

            session.Entity.Set<Account>(new(model));
            session.Entity.Set<Characters>(new(model, request.GateId, tables, new()));
            session.Entity.Set(GetBackground(model, tables));

            session.SendGateEnterResult().SendServiceCurrentDate();
        }

        private static CharacterBackgroundTableEntity GetBackground(AccountModel model, BinTables tables) => tables.CharacterBackgroundTable.TryGetValue(model.CharacterBackground, out CharacterBackgroundTableEntity entity)
            ? entity
            : tables.CharacterBackgroundTable.Values.First();
    }
}