using Grpc.Core;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.Protos;
using ow.Framework.IO.Network.Relay.Protos.Requests;
using ow.Framework.IO.Network.Relay.Protos.Responses;
using ow.Service.Relay.Repository;
using System.Threading.Tasks;

namespace ow.Service.Relay.Network.Relay.Handlers
{
    [GlobalHandler]
    internal class SessionHandler : SessionService.SessionServiceBase
    {
        public SessionHandler(SessionRepository repository) =>
            _repository = repository;

        public override Task<SessionRegisterResponse> Register(SessionRegisterRequest request, ServerCallContext context) =>
            Task.FromResult(new SessionRegisterResponse
            {
                Key = _repository.TryRegister(request.Account, out ulong key) ? key : ulong.MinValue
            });

        public override Task<SessionValidateResponse> Validate(SessionValidateRequest request, ServerCallContext context) =>
            Task.FromResult(new SessionValidateResponse
            {
                Result = _repository.Contains(request.Account, request.Key)
            });

        private readonly SessionRepository _repository;
    }
}