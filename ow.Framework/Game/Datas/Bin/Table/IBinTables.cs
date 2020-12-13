using ow.Framework.Game.Enums;
using System;
using System.Collections.Generic;

namespace ow.Framework.Game.Datas.Bin.Table
{
    public interface IBinTables
    {
        IReadOnlyDictionary<Hero, IClassSelectInfoTableEntity> ClassSelectInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, ICustomizeSkinTableEntity> CustomizeSkinTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, ICustomizeEyesTableEntity> CustomizeEyesTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, ICustomizeHairTableEntity> CustomizeHairTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<Hero, ICustomizeInfoTableEntity> CustomizeInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<uint, ICharacterBackgroundTableEntity> CharacterBackgroundTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, IDistrictTableEntity> DistrictTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<uint, IItemTableEntity> ItemTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, ICharacterInfoTableEntity> CharacterInfoTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<uint, IPhotoItemTableEntity> PhotoItemTable { get => throw new NotImplementedException(); }

        IReadOnlyDictionary<ushort, IGestureTableEntity> GestureTable { get => throw new NotImplementedException(); }
    }
}