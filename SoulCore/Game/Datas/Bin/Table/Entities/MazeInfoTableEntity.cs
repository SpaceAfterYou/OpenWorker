using SoulCore.Attributes;
using SoulCore.Extensions;
using System;
using System.IO;

namespace SoulCore.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt16;

    [BinTable("tb_Maze_Info")]
    public sealed record MazeInfoTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public byte Type { get; }
        public ushort Unknown7 { get; }
        public byte Unknown8 { get; }
        public byte MinLevel { get; }
        public byte Unknown10 { get; }
        public ushort Unknown11 { get; }
        public ushort Unknown12 { get; }
        public ushort Unknown13 { get; }
        public byte Unknown14 { get; }
        public byte EntranceCount { get; }
        public byte Unknown16 { get; }
        public byte Unknown17 { get; }
        public byte Unknown18 { get; }
        public byte Unknown19 { get; }
        public uint Ticket { get; }
        public byte Unknown21 { get; }
        public byte Unknown22 { get; }
        public byte Unknown23 { get; }
        public byte Unknown24 { get; }
        public byte Unknown25 { get; }
        public uint StartEventBox { get; }
        public ushort Unknown27 { get; }
        public ushort Unknown28 { get; }
        public ushort Unknown29 { get; }
        public string Batch { get; }
        public string Unknown31 { get; }
        public string LuaScript { get; }
        public string BGMMaze { get; }
        public string BGMBoss { get; }
        public string Map { get; }
        public ushort Unknown36 { get; }
        public ushort Unknown37 { get; }
        public byte Unknown38 { get; }
        public ushort Unknown39 { get; }
        public string LoadingBackground { get; }
        public string Unknown41 { get; }
        public ushort Unknown42 { get; }
        public ushort Portal { get; }
        public uint Unknown44 { get; }
        public uint Unknown45 { get; }
        public ushort District { get; }
        public uint Unknown47 { get; }
        public uint Unknown48 { get; }
        public uint Unknown49 { get; }
        public uint Unknown50 { get; }
        public uint Unknown51 { get; }
        public byte Unknown52 { get; }
        public byte Unknown53 { get; }
        public string Unknown54 { get; }
        public byte Unknown55 { get; }
        public uint Unknown56 { get; }
        public byte Unknown57 { get; }

        internal MazeInfoTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt16();
            Type = br.ReadByte();
            Unknown7 = br.ReadUInt16();
            Unknown8 = br.ReadByte();
            MinLevel = br.ReadByte();
            Unknown10 = br.ReadByte();
            Unknown11 = br.ReadUInt16();
            Unknown12 = br.ReadUInt16();
            Unknown13 = br.ReadUInt16();
            Unknown14 = br.ReadByte();
            EntranceCount = br.ReadByte();
            Unknown16 = br.ReadByte();
            Unknown17 = br.ReadByte();
            Unknown18 = br.ReadByte();
            Unknown19 = br.ReadByte();
            Ticket = br.ReadUInt32();
            Unknown21 = br.ReadByte();
            Unknown22 = br.ReadByte();
            Unknown23 = br.ReadByte();
            Unknown24 = br.ReadByte();
            Unknown25 = br.ReadByte();
            StartEventBox = br.ReadUInt32();
            Unknown27 = br.ReadUInt16();
            Unknown28 = br.ReadUInt16();
            Unknown29 = br.ReadUInt16();
            Batch = br.ReadByteLengthUnicodeString();
            Unknown31 = br.ReadByteLengthUnicodeString();
            LuaScript = br.ReadByteLengthUnicodeString();
            BGMMaze = br.ReadByteLengthUnicodeString();
            BGMBoss = br.ReadByteLengthUnicodeString();
            Map = br.ReadByteLengthUnicodeString();
            Unknown36 = br.ReadUInt16();
            Unknown37 = br.ReadUInt16();
            Unknown38 = br.ReadByte();
            Unknown39 = br.ReadUInt16();
            LoadingBackground = br.ReadByteLengthUnicodeString();
            Unknown41 = br.ReadByteLengthUnicodeString();
            Unknown42 = br.ReadUInt16();
            Portal = br.ReadUInt16();
            Unknown44 = br.ReadUInt32();
            Unknown45 = br.ReadUInt32();
            District = br.ReadUInt16();
            Unknown47 = br.ReadUInt32();
            Unknown48 = br.ReadUInt32();
            Unknown49 = br.ReadUInt32();
            Unknown50 = br.ReadUInt32();
            Unknown51 = br.ReadUInt32();
            Unknown52 = br.ReadByte();
            Unknown53 = br.ReadByte();
            Unknown54 = br.ReadByteLengthUnicodeString();
            Unknown55 = br.ReadByte();
            Unknown56 = br.ReadUInt32();
            Unknown57 = br.ReadByte();
        }
    }
}

// https://youtu.be/l74Ot_2kuNs
