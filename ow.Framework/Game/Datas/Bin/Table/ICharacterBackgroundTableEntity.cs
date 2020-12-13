using System;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = UInt32;

    public interface ICharacterBackgroundTableEntity : ITableEntity<KeyType>
    {
        ushort Unknown1 { get; }
        ushort Unknown2 { get; }
        ushort Unknown3 { get; }
        string Unknown4 { get; }
    }
}