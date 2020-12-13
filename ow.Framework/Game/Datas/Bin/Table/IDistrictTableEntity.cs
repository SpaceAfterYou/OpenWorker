using System;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = UInt16;

    public interface IDistrictTableEntity : ITableEntity<KeyType>
    {
        ushort Unknown5 { get; }
        ushort Unknown6 { get; }
        ushort Unknown7 { get; }
        string Unknown8 { get; }
        string Batch { get; }
        ushort Unknown10 { get; }
        uint Unknown11 { get; }
        uint Unknown12 { get; }
        string Bgm { get; }
        string Bg { get; }
        byte Unknown15 { get; }
        byte Unknown16 { get; }
        string Map { get; }
        byte Unknown18 { get; }
        byte Unknown19 { get; }
    }
}