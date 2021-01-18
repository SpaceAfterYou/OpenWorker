using System.Runtime.InteropServices;

namespace SoulCore.IO.Network.Sync.Responses.Shared
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct SMapInfoSharedResponse
    {
        [FieldOffset(0)]
        internal readonly ulong Seq;

        [FieldOffset(3)]
        public readonly byte ChannelId;

        [FieldOffset(4)]
        public readonly ushort MapId;

        [FieldOffset(6)]
        public readonly ushort ServerId;

        public SMapInfoSharedResponse(byte channelId, ushort mapId, ushort serverId)
        {
            Seq = 0;
            ChannelId = channelId;
            MapId = mapId;
            ServerId = serverId;
        }

        public static SMapInfoSharedResponse Empty => _empty;

        private static readonly SMapInfoSharedResponse _empty = new();
    }
}
