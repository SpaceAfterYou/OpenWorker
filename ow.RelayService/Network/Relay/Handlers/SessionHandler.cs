using Grpc.Core;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network.Relay.Attrubutes;
using ow.Framework.IO.Network.Relay.Protos;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ow.Service.Relay.Network.Relay.Handlers
{
    [Handler]
    internal class SessionHandler : SessionRelay.SessionRelayBase
    {
        private readonly ILogger<SessionHandler> _logger;

        private readonly ConcurrentDictionary<ulong, int> _sessions = new();
        private readonly RNGCryptoServiceProvider _rand = new();

        public SessionHandler(ILogger<SessionHandler> logger) => _logger = logger;

        public override Task<RegisterReply> Register(RegisterRequest request, ServerCallContext context)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            _rand.GetBytes(buffer, 0, sizeof(ulong));

            ulong key = BitConverter.ToUInt64(buffer);
            return Task.FromResult(new RegisterReply
            {
                Key = _sessions.TryAdd(key, request.Account) ? key : ulong.MinValue
            });
        }

        public override Task<ValidateReply> Validate(ValidateRequest request, ServerCallContext context) =>
            Task.FromResult(new ValidateReply
            {
                Result = _sessions.TryGetValue(request.Key, out int account) && account == request.Account
            });
    }
}