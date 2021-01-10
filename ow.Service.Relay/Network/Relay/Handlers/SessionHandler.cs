using Grpc.Core;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.Global.Protos.Requests;
using ow.Framework.IO.Network.Relay.Global.Protos.Responses;
using ow.Service.Relay.Repository;
using System.Threading.Tasks;
using static ow.Framework.IO.Network.Relay.Global.Protos.RGSSessionProto;

namespace ow.Service.Relay.Network.Relay.Handlers
{
    [GlobalHandler]
    internal sealed class SessionHandler : RGSSessionProtoBase
    {
        public SessionHandler(SessionRepository repository) =>
            _repository = repository;

        public override Task<RGSSessionRegisterResponse> Register(RGSSessionRegisterRequest request, ServerCallContext context) =>
            Task.FromResult(new RGSSessionRegisterResponse
            {
                Key = _repository.TryRegister(request.Account, out ulong key) ? key : ulong.MinValue
            });

        public override Task<RGSSessionValidateResponse> Validate(RGSSessionValidateRequest request, ServerCallContext context) =>
            Task.FromResult(new RGSSessionValidateResponse
            {
                Result = _repository.Contains(request.Account, request.Key)
            });

        private readonly SessionRepository _repository;
    }
}
