using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Game;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using ow.Service.Login.Game;
using ow.Service.Login.Game.Repositories;
using System.Linq;

namespace ow.Service.Login.Network.Handlers
{
    internal static class GateHandler
    {
        [Handler(ServerOpcode.GateList, HandlerPermission.Authorized)]
        public static void GetList(Session session, GateRepository gates, Features options) => session
            .SendAsync(GetPersonalGates(session, gates))
            .SendAsync(options);

        [Handler(ServerOpcode.GateConnect, HandlerPermission.Authorized)]
        public static void Connect(Session session, GateConnectRequest request, GateRepository gates) =>
            session.SendAsync(new AuthGateConnectionEndPointResponse
            {
                Ip = gates[request.Gate].Ip,
                Port = gates[request.Gate].Port
            });

        private static AuthPersonalGateResponse[] GetPersonalGates(Session session, GateRepository gates)
        {
            using CharacterContext context = new();
            return gates.Select(gate => GetPersonalGate(context, session, gate)).ToArray();
        }

        private static AuthPersonalGateResponse GetPersonalGate(CharacterContext context, Session session, GateInstance gate) =>
            new()
            {
                CharactersCount = (byte)GetCharactersCount(context, session, gate),
                Gate = new()
                {
                    Id = gate.Id,
                    Name = gate.Name,
                    Status = gate.Status,
                    PlayersOnlineCount = gate.PlayersOnlineCount,
                    EndPoint = new()
                    {
                        Ip = gate.Ip,
                        Port = gate.Port
                    },
                }
            };

        private static int GetCharactersCount(CharacterContext context, Session session, GateInstance gate) => context.Characters
            .Where(character => character.AccountId == session.Account.Id && character.Gate == gate.Id)
            .AsNoTracking()
            .Count();
    }
}