using StackExchange.Redis;

namespace Core.Systems.LanSystem
{
    public class LanContext
    {
        public ulong SetAccountIdBySessionKey(uint accountId)
        {
            //ulong sessionKey = (ulong)DateTimeOffset.UtcNow.Ticks;
            ulong sessionKey = 22;
            _multiplexer.GetDatabase().HashSet("SessionKey", sessionKey.ToString(), accountId);

            return sessionKey;
        }

        public uint GetAccountIdBySessionKey(ulong sessionId) =>
            (uint)_multiplexer.GetDatabase().HashGet("SessionKey", sessionId.ToString());

        public LanContext(ConnectionMultiplexer multiplexer) =>
            _multiplexer = multiplexer;

        private readonly ConnectionMultiplexer _multiplexer;
    }
}