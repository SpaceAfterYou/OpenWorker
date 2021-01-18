using Microsoft.EntityFrameworkCore;
using SoulCore.Database.Characters;
using SoulCore.Game;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using ow.Service.Auth.Game.Repositories;
using System.Linq;
using static SoulCore.IO.Network.Sync.Responses.SLUserCharacterForServerResponse;

namespace ow.Service.Auth.Network.Sync.Handlers
{
    public sealed class GateHandler
    {
        [Handler(ServerOpcode.GateList, HandlerPermission.Authorized)]
        public void GetCharacterCount(SyncSession session) => session
            .SendDeferred(new SLUserCharacterForServerResponse
            {
                Values = GetPersonalInfo(session)
            })
            .SendDeferred(_features);

        [Handler(ServerOpcode.GateConnect, HandlerPermission.Authorized)]
        public void Connect(SyncSession session, GateConnectRequest request)
        {
            GateRepository.Entity? gate = _repository.FirstOrDefault(s => s.Id == request.Gate);
            if (gate is null || gate.Status == GateStatus.Offline)
                return;

            session.SendDeferred(new AuthGateConnectionEndPointResponse
            {
                Ip = gate.Ip,
                Port = gate.Port
            });
        }

        public GateHandler(Options features, GateRepository repository, IDbContextFactory<CharacterContext> characterFactory)
        {
            _features = features;
            _repository = repository;
            _characterFactory = characterFactory;
        }

        private SLUCFSREntity[] GetPersonalInfo(SyncSession session)
        {
            using CharacterContext context = _characterFactory.CreateDbContext();
            return _repository.Select(s => GetPersonalGate(context, session, s)).ToArray();
        }

        private static SLUCFSREntity GetPersonalGate(CharacterContext context, SyncSession session, GateRepository.Entity gate) =>
            new()
            {
                CharactersCount = (byte)GetCharactersCount(context, session, gate),
                Id = gate.Id,
                Name = gate.Name,
                Status = gate.Status,
                PlayersOnlineCount = 0,
                EndPoint = new()
                {
                    Ip = gate.Ip,
                    Port = gate.Port
                },
            };

        private static int GetCharactersCount(CharacterContext context, SyncSession session, GateRepository.Entity gate) => context.Characters
            .AsNoTracking()
            .Count(character => character.AccountId == session.Account.Id && character.Gate == gate.Id);

        private readonly Options _features;
        private readonly GateRepository _repository;
        private readonly IDbContextFactory<CharacterContext> _characterFactory;
    }
}
