namespace ow.Framework.IO.Network.Responses
{
    public sealed record BoosterAddResponse
    {
        public byte Id { get; }
        public ushort PrototypeId { get; }
        public ulong Duration { get; }
    }
}