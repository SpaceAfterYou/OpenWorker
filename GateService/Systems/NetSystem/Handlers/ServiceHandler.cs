using Core.Systems.DatabaseSystem.Accounts;
using Core.Systems.LanSystem;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Gate;
using GateService.Systems.GameSystem;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GateService.Systems.NetSystem.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(HandlerOpcode.GateEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, EnterRequest request, Gate gate, LanContext lan)
        {
            if (gate.Id != request.GateId)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            uint accountId = lan.GetAccountIdBySessionKey(request.SessionKey);
            if (request.AccountId != accountId)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            using AccountContext context = new();

            AccountModel model = context.Accounts.AsNoTracking().FirstOrDefault(c => c.Id == request.AccountId);
            if (model is null)
            {
#if !DEBUG
                session.Disconnect();
#endif
                return;
            }

            session.Account = new(model);
            session.Characters = new(request.AccountId, request.GateId);

            session.SendGateEnterResult().SendCurrentDate();
        }
    }
}