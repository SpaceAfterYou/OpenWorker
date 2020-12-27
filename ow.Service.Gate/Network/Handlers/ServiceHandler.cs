using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.Utils;
using ow.Service.Gate.Game;
using System.Linq;

namespace ow.Service.Gate.Network.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(ServerOpcode.GateEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, GateEnterRequest request, GateInstance gate, LanContext lan, BinTables tables)
        {
            if (gate.Id != request.Gate)
                NetworkUtils.DropSession();

            if (request.Account != lan.GetAccountIdBySessionKey(request.SessionKey))
                NetworkUtils.DropSession();

            {
                using AccountContext context = new();

                AccountModel model = context.Accounts.AsNoTracking().First(c => c.Id == request.Account);

                session.Account = new(model);
                session.Characters = new(model, request.Gate, tables);
                session.Background = model.CharacterBackground;
            }

            session.SendAsync(new GateEnterResponse() { AccountId = request.Account, Result = GateEnterResult.Success });
            session.SendAsync(new ServiceCurrentDataResponse());
        }
    }
}
