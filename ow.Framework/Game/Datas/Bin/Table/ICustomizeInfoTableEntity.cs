using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = Hero;

    public interface ICustomizeInfoTableEntity : ITableEntity<KeyType>
    {
        byte Unknown1 { get; }
        IReadOnlyList<byte> Unknown2 { get; }
        IReadOnlyList<byte> Unknown3 { get; }
        IReadOnlyList<string> Unknown4 { get; }
    }
}