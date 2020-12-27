using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Game;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Service.Auth.Game;
using ow.Service.Auth.Game.Repositories;
using System.Linq;

namespace ow.Service.Auth.Network.Handlers
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
