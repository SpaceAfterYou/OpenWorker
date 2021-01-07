namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record BattlePassLoadResponse
    {
        public uint Id;
        public byte NextReward; // IDK
        public long StartDate;
        public long EndDate;
        public uint HavePoint;
        public byte IsPremium;
    }
}