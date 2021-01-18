﻿using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct MazeCreateRequest
    {
        public byte Unknown1 { get; }
        public uint Unknown2 { get; }
        public uint Unknown3 { get; }
        public uint Unknown4 { get; }
        public ushort Maze { get; }

        public MazeCreateRequest(BinaryReader br)
        {
            Unknown1 = br.ReadByte();
            Unknown2 = br.ReadUInt32();
            Unknown3 = br.ReadUInt32();
            Unknown4 = br.ReadUInt32();
            Maze = br.ReadUInt16();
        }
    }
}
