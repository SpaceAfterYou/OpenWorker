using ow.Framework.Attributes;
using ow.Framework.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ow.Framework.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    [BinTable("tb_Character_Info")]
    public sealed record CharacterInfoTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public ushort Unknown0 { get; }
        public byte Unknown1 { get; }
        public string Unknown2 { get; }
        public string Unknown3 { get; }
        public string DialogueTalkImgPath { get; }
        public string Token { get; }
        public string Unknown4 { get; }
        public string CutScenePath { get; }
        public string GhostTalkImgPath { get; }
        public uint Unknown5 { get; }
        public uint Unknown6 { get; }
        public uint Unknown7 { get; }
        public uint Unknown8 { get; }
        public uint Unknown9 { get; }
        public uint Unknown10 { get; }
        public uint Unknown11 { get; }
        public uint Unknown12 { get; }
        public uint Unknown13 { get; }
        public ushort Unknown24 { get; }
        public ushort Unknown25 { get; }
        public uint DefaultWeapon { get; }
        public IReadOnlyList<uint> DefaultOutfits { get; }
        public uint ProvideItem { get; }
        public short District { get; }
        public short DistrictPositionX { get; }
        public short DistrictPositionY { get; }
        public short DistrictPositionZ { get; }
        public short Maze { get; }
        public byte MazeSpawnBox { get; }
        public uint StartQuest { get; }
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
        public IReadOnlyList<uint> DefaultSkill { get; }
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
            Unknown0 = br.ReadUInt16();
            Unknown1 = br.ReadByte();
            Unknown2 = br.ReadByteLengthUnicodeString();
            Unknown3 = br.ReadByteLengthUnicodeString();
            DialogueTalkImgPath = br.ReadByteLengthUnicodeString();
            Token = br.ReadByteLengthUnicodeString();
            Unknown4 = br.ReadByteLengthUnicodeString();
            CutScenePath = br.ReadByteLengthUnicodeString();
            GhostTalkImgPath = br.ReadByteLengthUnicodeString();
            Unknown5 = br.ReadUInt32();
            Unknown6 = br.ReadUInt32();
            Unknown7 = br.ReadUInt32();
            Unknown8 = br.ReadUInt32();
            Unknown9 = br.ReadUInt32();
            Unknown10 = br.ReadUInt32();
            Unknown11 = br.ReadUInt32();
            Unknown12 = br.ReadUInt32();
            Unknown13 = br.ReadUInt32();
            Unknown24 = br.ReadUInt16();
            Unknown25 = br.ReadUInt16();
            DefaultWeapon = br.ReadUInt32();
            DefaultOutfits = br.ReadUInt32Array(DefaultCostumesCount);
            ProvideItem = br.ReadUInt32();
            District = br.ReadInt16();
            DistrictPositionX = br.ReadInt16();
            DistrictPositionY = br.ReadInt16();
            DistrictPositionZ = br.ReadInt16();
            Maze = br.ReadInt16();
            MazeSpawnBox = br.ReadByte();
            StartQuest = br.ReadUInt32();
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
            DefaultSkill = br.ReadUInt32Array(DefaultPassiveSkillCount);
            Unknown85 = br.ReadUInt16();
            Unknown86 = br.ReadSingle();
            Unknown87 = br.ReadUInt32();
            Unknown88 = br.ReadUInt32();
            Unknown89 = br.ReadUInt16();
            Unknown90 = br.ReadByte();
        }

        private const byte DefaultGesturesCount = 9;
        private const byte DefaultCostumesCount = 4;
        private const byte DefaultPassiveSkillCount = 20;
    }
}