using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using System;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    public interface IBinTables
    {
        IReadOnlyDictionary<Hero, ClassSelectInfoTableEntity> ClassSelectInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, CustomizeSkinTableEntity> CustomizeSkinTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, CustomizeEyesTableEntity> CustomizeEyesTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, CustomizeHairTableEntity> CustomizeHairTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, CustomizeInfoTableEntity> CustomizeInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<uint, CharacterBackgroundTableEntity> CharacterBackgroundTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<uint, ItemTableEntity> ItemTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, CharacterInfoTableEntity> CharacterInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<uint, PhotoItemTableEntity> PhotoItemTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, GestureTableEntity> GestureTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, MazeInfoTableEntity> MazeInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, BoosterTableEntity> BoosterTable { get => throw new NotImplementedException(); }
    }
}

/// https://youtu.be/tcmq8483LKg