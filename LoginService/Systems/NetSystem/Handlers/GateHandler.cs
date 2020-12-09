using Core.Systems.DatabaseSystem.Characters;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests;
using LoginService.Systems.GameSystem;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LoginService.Systems.NetSystem.Handlers
{
    internal static class GateHandler
    {
        [Handler(HandlerOpcode.GateList, HandlerPermission.Authorized)]
        public static void GetList(Session session, Gates gates, Options options) => session
            .SendGateList(GetPersonalGates(session.Account, gates))
            .SendOptionLoad(options);

        [Handler(HandlerOpcode.GateConnect, HandlerPermission.Authorized)]
        public static void Connect(Session session, ConnectRequest request, Gates gates) =>
            session.SendGateConnect(gates[request.GateId]);

        private static PersonalGate[] GetPersonalGates(Account account, Gates gates)
        {
            using CharacterContext context = new();
            return gates.Select(gate => GetPersonalGate(context, account, gate)).ToArray();
        }

        private static PersonalGate GetPersonalGate(CharacterContext context, Account account, Gate gate) =>
            new(gate, (byte)GetCharactersCount(context, account, gate));

        private static int GetCharactersCount(CharacterContext context, Account account, Gate gate) => context.Characters
            .Where(character => character.AccountId == account.Id && character.GateId == gate.Id)
            .AsNoTracking()
            .Count();
    }
}