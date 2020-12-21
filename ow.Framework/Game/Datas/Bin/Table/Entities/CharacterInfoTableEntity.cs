using ow.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    public sealed record CharacterInfoTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public ushort Unknown6 { get; }
        public byte Unknown7 { get; }
        public string Unknown8 { get; }
        public string Unknown9 { get; }
        public string DialogueTalkImgPath { get; }
        public string Token { get; }
        public string Unknown12 { get; }
        public string CutScenePath { get; }
        public string GhostTalkImgPath { get; }
        public IReadOnlyList<uint> Unknown13 { get; }
        public ushort Unknown24 { get; }
        public ushort Unknown25 { get; }
        public uint DefaultWeapon { get; }
        public IReadOnlyList<uint> DefaultOutfits { get; }
        public uint Unknown31 { get; }
        public short Unknown32 { get; }
        public short Unknown33 { get; }
        public short Unknown34 { get; }
        public short Unknown35 { get; }
        public short Maze { get; }
        public byte SpawnBox { get; }
        public uint Unknown38 { get; }
        public uint Unknown39 { get; }
        public uint Unknown40 { get; }
        public ushort Unknown41 { get; }
        public float Unknown42 { get; }
        public short Unknown43 { get; }
        public ushort Unknown44 { get; }
        public ushort Unknown45 { get; }
        public ushort Unknown46 { get; }
        public ushort Unknown47 { get; }
        public ushort Unknown48 { get; }
        public ushort Unknown49 { get; }
        public float Unknown50 { get; }
        public byte Unknown51 { get; }
        public ushort Unknown52 { get; }
        public ushort Unknown53 { get; }
        public ushort Unknown54 { get; }
        public ushort Unknown55 { get; }
        public ushort Unknown56 { get; }
        public ushort Unknown57 { get; }
        public ushort Unknown58 { get; }
        public ushort Unknown59 { get; }
        public ushort Unknown60 { get; }
        public short Unknown61 { get; }
        public ushort Unknown62 { get; }
        public ushort Unknown63 { get; }
        public ushort Unknown64 { get; }
        public IReadOnlyList<uint> DefaultPassiveSkill { get; }
        public IReadOnlyList<uint> DefaultActiveSkill { get; }
        public IReadOnlyList<uint> DefaultGenericSkill { get; }
        public uint Unknown77 { get; }
        public uint Unknown78 { get; }
        public uint Unknown79 { get; }
        public uint Unknown80 { get; }
        public uint Unknown81 { get; }
        public uint Unknown82 { get; }
        public uint Unknown83 { get; }
        public uint Unknown84 { get; }
        public ushort Unknown85 { get; }
        public float Unknown86 { get; }
        public uint Unknown87 { get; }
        public uint Unknown88 { get; }
        public ushort Unknown89 { get; }
        public byte Unknown90 { get; }

        internal CharacterInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            Unknown6 = br.ReadUInt16();
            Unknown7 = br.ReadByte();
            Unknown8 = br.ReadByteLengthUnicodeString();
            Unknown9 = br.ReadByteLengthUnicodeString();
            DialogueTalkImgPath = br.ReadByteLengthUnicodeString();
            Token = br.ReadByteLengthUnicodeString();
            Unknown12 = br.ReadByteLengthUnicodeString();
            CutScenePath = br.ReadByteLengthUnicodeString();
            GhostTalkImgPath = br.ReadByteLengthUnicodeString();
            Unknown13 = br.ReadUInt32Array(DefaultGesturesCount);
            Unknown24 = br.ReadUInt16();
            Unknown25 = br.ReadUInt16();
            DefaultWeapon = br.ReadUInt32();
            DefaultOutfits = br.ReadUInt32Array(DefaultCostumesCount);
            Unknown31 = br.ReadUInt32();
            Unknown32 = br.ReadInt16();
            Unknown33 = br.ReadInt16();
            Unknown34 = br.ReadInt16();
            Unknown35 = br.ReadInt16();
            Maze = br.ReadInt16();
            SpawnBox = br.ReadByte();
            Unknown38 = br.ReadUInt32();
            Unknown39 = br.ReadUInt32();
            Unknown40 = br.ReadUInt32();
            Unknown41 = br.ReadUInt16();
            Unknown42 = br.ReadSingle();
            Unknown43 = br.ReadInt16();
            Unknown44 = br.ReadUInt16();
            Unknown45 = br.ReadUInt16();
            Unknown46 = br.ReadUInt16();
            Unknown47 = br.ReadUInt16();
            Unknown48 = br.ReadUInt16();
            Unknown49 = br.ReadUInt16();
            Unknown50 = br.ReadSingle();
            Unknown51 = br.ReadByte();
            Unknown52 = br.ReadUInt16();
            Unknown53 = br.ReadUInt16();
            Unknown54 = br.ReadUInt16();
            Unknown55 = br.ReadUInt16();
            Unknown56 = br.ReadUInt16();
            Unknown57 = br.ReadUInt16();
            Unknown58 = br.ReadUInt16();
            Unknown59 = br.ReadUInt16();
            Unknown60 = br.ReadUInt16();
            Unknown61 = br.ReadInt16();
            Unknown62 = br.ReadUInt16();
            Unknown63 = br.ReadUInt16();
            Unknown64 = br.ReadUInt16();
            DefaultPassiveSkill = br.ReadUInt32Array(DefaultPassiveSkillCount);
            DefaultActiveSkill = br.ReadUInt32Array(DefaultActiveSkillCount);
            DefaultGenericSkill = br.ReadUInt32Array(DefaultGenericSkillCount);
            Unknown77 = br.ReadUInt32();
            Unknown78 = br.ReadUInt32();
            Unknown79 = br.ReadUInt32();
            Unknown80 = br.ReadUInt32();
            Unknown81 = br.ReadUInt32();
            Unknown82 = br.ReadUInt32();
            Unknown83 = br.ReadUInt32();
            Unknown84 = br.ReadUInt32();
            Unknown85 = br.ReadUInt16();
            Unknown86 = br.ReadSingle();
            Unknown87 = br.ReadUInt32();
            Unknown88 = br.ReadUInt32();
            Unknown89 = br.ReadUInt16();
            Unknown90 = br.ReadByte();
        }

        private const byte DefaultGesturesCount = 9;
        private const byte DefaultCostumesCount = 4;
        private const byte DefaultPassiveSkillCount = 6;
        private const byte DefaultActiveSkillCount = 3;
        private const byte DefaultGenericSkillCount = 3;
    }
}