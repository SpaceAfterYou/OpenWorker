using Core.DatabaseSystem.Accounts;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Gate;
using GateService.Systems.GameSystem;
using GateService.Systems.NetSystem.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GateService.Systems.NetSystem.Handlers
{
    internal static class ServerHandler
    {
        [Handler(HandlerOpcode.GateEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, EnterRequest request, Gate gate)
        {
            if (gate.Id != request.GateId)
            {
                //session.Disconnect();
                return;
            }

            AccountModel account = GetAccount(request.AccountId, request.SessionKey);
            if (account is null)
            {
                //session.Disconnect();
                return;
            }

            session.Account = new(account);
            session.Characters = new(request.AccountId, request.GateId);

            session.SendGateEnterResult().SendCurrentDate();
        }

        private static AccountModel GetAccount(uint id, ulong sessionKey)
        {
            using AccountContext context = new();
            return context.Accounts.AsNoTracking().FirstOrDefault(c => c.Id == id && c.SessionKey == sessionKey);
        }
    }
}