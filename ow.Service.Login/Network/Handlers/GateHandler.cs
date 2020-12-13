using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Game;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Service.Login.Game;
using ow.Service.Login.Network.Extensions;
using System.Linq;

namespace ow.Service.Login.Network.Handlers
{
    internal static class GateHandler
    {
        [Handler(ServerOpcode.GateList, HandlerPermission.Authorized)]
        public static void GetList(GameSession session, GatesInstances gates, OptionsStatuses options) => session
            .SendGateList(GetPersonalGates(session.Entity.Get<Account>(), gates))
            .SendGameOptions(options);

        [Handler(ServerOpcode.GateConnect, HandlerPermission.Authorized)]
        public static void Connect(GameSession session, ConnectRequest request, GatesInstances gates) =>
            session.SendGateConnect(gates[request.GateId]);

        private static PersonalGate[] GetPersonalGates(Account account, GatesInstances gates)
        {
            using CharacterContext context = new();
            return gates.Select(gate => GetPersonalGate(context, account, gate)).ToArray();
        }

        private static PersonalGate GetPersonalGate(CharacterContext context, Account account, GateInstance gate) =>
            new(gate, (byte)GetCharactersCount(context, account, gate));

        private static int GetCharactersCount(CharacterContext context, Account account, GateInstance gate) => context.Characters
            .Where(character => character.AccountId == account.Id && character.GateId == gate.Id)
            .AsNoTracking()
            .Count();
    }
}