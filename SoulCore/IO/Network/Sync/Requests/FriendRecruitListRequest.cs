﻿using System.IO;
using SoulCore.IO.Network.Sync.Attributes;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct FriendRecruitListRequest
    {
        public ushort Unknown1 { get; }
        public ushort Unknown2 { get; }
        public ushort Unknown3 { get; }
        public byte Unknown4 { get; }

        public FriendRecruitListRequest(BinaryReader br)
        {
            Unknown1 = br.ReadUInt16();
            Unknown2 = br.ReadUInt16();
            Unknown3 = br.ReadUInt16();
            Unknown4 = br.ReadByte();
        }
    }
}
