﻿using SoulCore.Attributes;
using SoulCore.Extensions;
using System;
using System.IO;

namespace SoulCore.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_Character_Background")]
    public sealed record CharacterBackgroundTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public ushort Unknown1 { get; }
        public ushort Unknown2 { get; }
        public ushort Unknown3 { get; }
        public string Unknown4 { get; }

        internal CharacterBackgroundTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            Unknown1 = br.ReadUInt16();
            Unknown2 = br.ReadUInt16();
            Unknown3 = br.ReadUInt16();
            Unknown4 = br.ReadByteLengthUnicodeString();
        }
    }
}
