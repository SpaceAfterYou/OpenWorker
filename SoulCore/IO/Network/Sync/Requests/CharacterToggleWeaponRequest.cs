﻿using SoulCore.Extensions;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;
using System.Numerics;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public sealed record CharacterToggleWeaponRequest
    {
        public int Character { get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public uint Toggle { get; }
        public uint Unknown1 { get; }

        public CharacterToggleWeaponRequest(BinaryReader br)
        {
            Character = br.ReadInt32();
            Position = br.ReadVector3();
            Rotation = br.ReadSingle();
            Toggle = br.ReadUInt32();
            Unknown1 = br.ReadUInt32();
        }
    }
}
