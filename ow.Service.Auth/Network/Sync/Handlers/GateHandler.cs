using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Characters;
using ow.Framework.Game;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Service.Auth.Game.Repositories;
using System;
using System.Linq;

namespace ow.Service.Auth.Network.Sync.Handlers
{
    public sealed class GateHandler
    {
        [Handler(ServerOpcode.GateList, HandlerPermission.Authorized)]
        public void GetList(Session session) => session
            .SendAsync(GetPersonalInfo(session))
            .SendAsync(_features);

        [Handler(ServerOpcode.GateConnect, HandlerPermission.Authorized)]
        public void Connect(Session session, GateConnectRequest request)
        {
            GateRepository.Entity? gate = _repository.FirstOrDefault(s => s.Id == request.Gate);
            if (gate is null || gate.Status == GateStatus.Online)
                return;

            session.SendAsync(new AuthGateConnectionEndPointResponse
            {
                Ip = gate.Ip,
                Port = gate.Port
            });
        }

        public GateHandler(Features features, GateRepository repository, Func<CharacterContext> characterFactory)
        {
            _features = features;
            _repository = repository;
            _characterFactory = characterFactory;
        }

        private AuthPersonalGateResponse[] GetPersonalInfo(Session session)
        {
            using CharacterContext context = _characterFactory();
            return _repository.Select(s => GetPersonalGate(context, session, s)).ToArray();
        }

        private static AuthPersonalGateResponse GetPersonalGate(CharacterContext context, Session session, GateRepository.Entity gate) =>
            new()
            {
                CharactersCount = (byte)GetCharactersCount(context, session, gate),
                Gate = new()
                {
                    Id = gate.Id,
                    Name = gate.Name,
                    Status = gate.Status,
                    PlayersOnlineCount = 0,
                    EndPoint = new()
                    {
                        Ip = gate.Ip,
                        Port = gate.Port
                    },
                }
            };

        private static int GetCharactersCount(CharacterContext context, Session session, GateRepository.Entity gate) => context.Characters
            .AsNoTracking()
            .Count(character => character.AccountId == session.Account.Id && character.Gate == gate.Id);

        private readonly Features _features;
        private readonly GateRepository _repository;
        private readonly Func<CharacterContext> _characterFactory;
    }
}