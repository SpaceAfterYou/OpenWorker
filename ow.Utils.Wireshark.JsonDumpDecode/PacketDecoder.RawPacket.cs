namespace ow.Utils.Wireshark.JsonDumpDecode
{
    public sealed partial class PacketDecoder
    {
        public sealed record RawPacket
        {
            public string Frame { get; init; }
            public string RelativeTime { get; init; }
            public string SrcIp { get; init; }
            public string DstIp { get; init; }
            public string Payload { get; init; }
        }
    }
}