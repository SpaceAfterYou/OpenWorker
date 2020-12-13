using ow.Framework.Game.Enums;
using System;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = UInt32;

    public interface IPhotoItemTableEntity : ITableEntity<KeyType>
    {
        public uint Unknown1 { get; }
        public ushort Unknown2 { get; }
        public string Unknown3 { get; }
        public string Unknown4 { get; }
        public string Unknown5 { get; }
        public string Unknown6 { get; }
        public string Unknown7 { get; }
        public string Unknown8 { get; }
        public string Unknown9 { get; }
        public string Unknown10 { get; }
        public string Unknown11 { get; }
        public string Unknown12 { get; }
        public Hero Hero { get; }
        public byte Unknown14 { get; }
    }
}

// https://youtu.be/l74Ot_2kuNs