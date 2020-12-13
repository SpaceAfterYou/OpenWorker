using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    using KeyType = Hero;

    public interface ICustomizeHairTableEntity : ITableEntity<KeyType>
    {
        IReadOnlyList<uint> Unknown1 { get; }
        IReadOnlyList<uint> Style { get; }
        IReadOnlyList<uint> Unknown2 { get; }
        IReadOnlyList<string> Icons { get; }
        IReadOnlyList<uint> Color { get; }
    }
}