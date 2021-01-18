﻿using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.Extensions;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct ServiceKeepAliveRequest
    {
        public uint Seed { get; }
        public uint Unknown1 { get; }
        public uint Unknown2 { get; }
        public uint Unknown3 { get; }
        public ulong TickCount { get; }
        public string Hash { get; }

        public ServiceKeepAliveRequest(BinaryReader br)
        {
            Seed = br.ReadUInt32();
            Unknown1 = br.ReadUInt32();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
            TickCount = br.ReadUInt64();
            Hash = br.ReadNumberLengthUtf8String();
        }
    }
}
