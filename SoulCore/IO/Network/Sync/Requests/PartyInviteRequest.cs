﻿using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.Extensions;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public readonly struct PartyInviteRequest
    {
        public string CharacterName { get; }
        public short Unknown1 { get; } // location id???
        public int CharacterId { get; }
        public int Unknown2 { get; }
        public int Unknown3 { get; }
        public int Unknown4 { get; }
        public byte Unknown5 { get; }

        public PartyInviteRequest(BinaryReader br)
        {
            CharacterName = br.ReadNumberLengthUnicodeString();
            Unknown1 = br.ReadInt16();
            CharacterId = br.ReadInt32();
            Unknown2 = br.ReadInt32();
            Unknown3 = br.ReadInt32();
            Unknown4 = br.ReadInt32();
            Unknown5 = br.ReadByte();
        }
    }
}
