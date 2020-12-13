using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = Hero;

    public interface ICustomizeSkinTableEntity : ITableEntity<KeyType>
    {
        IReadOnlyList<uint> Unknown1 { get; }
        IReadOnlyList<string> Icons { get; }
        IReadOnlyList<uint> Color { get; }
    }
}