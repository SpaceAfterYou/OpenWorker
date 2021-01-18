﻿using System.IO;
using SoulCore.IO.Network.Sync.Attributes;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct Unknown1Request
    {
        public byte Unknown1 { get; }

        public Unknown1Request(BinaryReader br) =>
            Unknown1 = br.ReadByte();
    }
}
