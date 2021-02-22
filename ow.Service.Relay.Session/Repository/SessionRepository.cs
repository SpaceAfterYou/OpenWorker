using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ow.Service.Relay.Session.Repository
{
    public sealed class SessionRepository
    {
        private readonly ConcurrentDictionary<ulong, int> _keys = new();
        private readonly RNGCryptoServiceProvider _rand = new();
        private readonly ILogger<SessionRepository> _logger;

        public SessionRepository(ILogger<SessionRepository> logger) => _logger = logger;

        internal bool TryRegister(int account, out ulong key)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            _rand.GetBytes(buffer, 0, sizeof(ulong));

            key = BitConverter.ToUInt64(buffer);
            _logger.LogDebug($"Create session key: {key}");

            return _keys.TryAdd(key, account);
        }

        internal bool Contains(int account, ulong key) => _keys.TryGetValue(key, out int a) && a == account;
    }
}