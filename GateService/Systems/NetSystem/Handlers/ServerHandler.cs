using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Gate;

namespace GateService.Systems.NetSystem.Handlers
{
    internal static class ServerHandler
    {
        [Handler(HandlerOpcode.GateEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, EnterRequest request, GateInfo gate, CharactersFactory charactersFactory)
        {
            if (gate.Id != request.GateId)
            {
                session.Disconnect();
                return;
            }

            session.Characters = new();

            var accountModel = await GateEnterHelper.LoadAccount(request.AccountId, request.SessionKey);

            var slots = await charactersFactory
                .UseAccountId(accountModel.Id)
                .UseGateId(request.GateId)
                .UseLastCharacterId(accountModel.LastSelectedCharacter)
                .Create();

            session
                .UseComponent(new Account(accountModel))
                .UseComponent(slots)
                .UseEventGroup<AuthorizedGroupAttribute>()
                .SendEnterResultGate()
                .SendCurrentDate();
        }
    }
}