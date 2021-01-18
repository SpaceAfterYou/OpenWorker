using SoulCore.Attributes;
using SoulCore.Extensions;
using System;
using System.IO;

namespace SoulCore.Game.Datas.Bin.Table.Entities
{
    using KeyType = UInt32;

    [BinTable("tb_Npc")]
    public sealed record NpcTableEntity : ITableEntity<KeyType>
    {
        public KeyType Id { get; }
        public ushort v6 { get; }
        public byte v7 { get; }
        public string v8 { get; }
        public uint v9 { get; }
        public uint v10 { get; }
        public uint v11 { get; }
        public uint v12 { get; }
        public uint v13 { get; }
        public byte v14 { get; }
        public uint v15 { get; }
        public byte v16 { get; }
        public byte v17 { get; }
        public uint v18 { get; }
        public byte v19 { get; }
        public uint v20 { get; }
        public byte v21 { get; }
        public uint v22 { get; }
        public ushort v23 { get; }
        public ushort v24 { get; }
        public ushort v25 { get; }
        public byte v26 { get; }
        public byte v27 { get; }
        public byte v28 { get; }
        public byte v29 { get; }
        public byte v30 { get; }
        public byte v31 { get; }
        public ushort v32 { get; }
        public ushort v33 { get; }
        public ushort v34 { get; }
        public string v35 { get; }
        public byte v36 { get; }
        public uint v37 { get; }
        public uint v38 { get; }
        public string v39 { get; }
        public ushort v40 { get; }
        public ushort v41 { get; }
        public ushort v42 { get; }
        public ushort v43 { get; }
        public ushort v44 { get; }
        public ushort v45 { get; }
        public ushort v46 { get; }
        public ushort v47 { get; }
        public ushort v48 { get; }
        public ushort v49 { get; }
        public ushort v50 { get; }
        public ushort v51 { get; }
        public ushort v52 { get; }
        public ushort v53 { get; }
        public ushort v54 { get; }
        public ushort v55 { get; }
        public string v56 { get; }
        public string v57 { get; }
        public string v58 { get; }
        public string v59 { get; }
        public string v60 { get; }

        internal NpcTableEntity(BinaryReader br)
        {
            Id = br.ReadUInt32();
            v6 = br.ReadUInt16();
            v7 = br.ReadByte();
            v8 = br.ReadByteLengthUnicodeString();
            v9 = br.ReadUInt32();
            v10 = br.ReadUInt32();
            v11 = br.ReadUInt32();
            v12 = br.ReadUInt32();
            v13 = br.ReadUInt32();
            v14 = br.ReadByte();
            v15 = br.ReadUInt32();
            v16 = br.ReadByte();
            v17 = br.ReadByte();
            v18 = br.ReadUInt32();
            v19 = br.ReadByte();
            v20 = br.ReadUInt32();
            v21 = br.ReadByte();
            v22 = br.ReadUInt32();
            v23 = br.ReadUInt16();
            v24 = br.ReadUInt16();
            v25 = br.ReadUInt16();
            v26 = br.ReadByte();
            v27 = br.ReadByte();
            v28 = br.ReadByte();
            v29 = br.ReadByte();
            v30 = br.ReadByte();
            v31 = br.ReadByte();
            v32 = br.ReadUInt16();
            v33 = br.ReadUInt16();
            v34 = br.ReadUInt16();
            v35 = br.ReadByteLengthUnicodeString();
            v36 = br.ReadByte();
            v37 = br.ReadUInt32();
            v38 = br.ReadUInt32();
            v39 = br.ReadByteLengthUnicodeString();
            v40 = br.ReadUInt16();
            v41 = br.ReadUInt16();
            v42 = br.ReadUInt16();
            v43 = br.ReadUInt16();
            v44 = br.ReadUInt16();
            v45 = br.ReadUInt16();
            v46 = br.ReadUInt16();
            v47 = br.ReadUInt16();
            v48 = br.ReadUInt16();
            v49 = br.ReadUInt16();
            v50 = br.ReadUInt16();
            v51 = br.ReadUInt16();
            v52 = br.ReadUInt16();
            v53 = br.ReadUInt16();
            v54 = br.ReadUInt16();
            v55 = br.ReadUInt16();
            v56 = br.ReadByteLengthUnicodeString();
            v57 = br.ReadByteLengthUnicodeString();
            v58 = br.ReadByteLengthUnicodeString();
            v59 = br.ReadByteLengthUnicodeString();
            v60 = br.ReadByteLengthUnicodeString();
        }
    }
}

// https://youtu.be/ze0HkbcF3_g
