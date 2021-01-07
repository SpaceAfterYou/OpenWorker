namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record BattlePassLoadResponse
    {
        public uint Id;
        public byte NextReward;
        public uint HavePoint;
    }
}