namespace ow.Utils.Wireshark.JsonDumpDecode
{
    public sealed partial class PacketDecoder
    {
        public sealed record Packet
        {
            public long Frame { get; init; }
            public string Time { get; init; }
            public string Sender { get; init; }
            public string Receiver { get; init; }
            public byte[] Body { get; init; }
        }
    }
}