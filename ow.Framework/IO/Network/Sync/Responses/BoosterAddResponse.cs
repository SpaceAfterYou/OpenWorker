namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record BoosterAddResponse
    {
        public byte Id { get; }
        public ushort PrototypeId { get; }
        public ulong Duration { get; }
    }
}
