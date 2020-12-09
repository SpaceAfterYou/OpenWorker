using ow.Framework.Database.Characters;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using LoginService.Game;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LoginService.Network.Handlers
{
    internal static class GateHandler
    {
        [Handler(ServerOpcode.GateList, HandlerPermission.Authorized)]
        public static void GetList(Session session, Gates gates, Options options) => session
            .SendGateList(GetPersonalGates(session.Account, gates))
            .SendOptionLoad(options);

        [Handler(ServerOpcode.GateConnect, HandlerPermission.Authorized)]
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
