using ow.Framework.Game.Enums;
using System;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = UInt16;

    public interface IGestureTableEntity : ITableEntity<KeyType>
    {
        Hero Hero { get; }
        byte Unknown2 { get; }
        byte Unknown3 { get; }
        uint Unknown4 { get; }
        ushort Unknown5 { get; }
        uint Unknown6 { get; }
        string Unknown7 { get; }
        byte Unknown8 { get; }
        uint Unknown9 { get; }
        uint Unknown10 { get; }
        uint Unknown11 { get; }
        uint Unknown12 { get; }
        string Unknown13 { get; }
        string Unknown14 { get; }
        string Unknown15 { get; }
        uint Unknown16 { get; }
        ushort Unknown17 { get; }
        ushort Unknown18 { get; }
    }
}