﻿using Microsoft.Extensions.Configuration;
using SoulCore.Attributes;
using SoulCore.Game.Datas.Bin;
using SoulCore.Game.Datas.Bin.Table.Entities;
using SoulCore.Game.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SoulCore.IO.File.Bin
{
    public sealed class BinTable : IDisposable
    {
        private readonly BinFile _data;

        public IReadOnlyDictionary<Hero, ClassSelectInfoTableEntity> ReadClassSelectInfoTable() =>
            Read<Hero, ClassSelectInfoTableEntity>(_data);

        public IReadOnlyDictionary<Hero, CustomizeSkinTableEntity> ReadCustomizeSkinTable() =>
            Read<Hero, CustomizeSkinTableEntity>(_data);

        public IReadOnlyDictionary<Hero, CustomizeEyesTableEntity> ReadCustomizeEyesTable() =>
            Read<Hero, CustomizeEyesTableEntity>(_data);

        public IReadOnlyDictionary<Hero, CustomizeHairTableEntity> ReadCustomizeHairTable() =>
            Read<Hero, CustomizeHairTableEntity>(_data);

        public IReadOnlyDictionary<Hero, CustomizeInfoTableEntity> ReadCustomizeInfoTable() =>
            Read<Hero, CustomizeInfoTableEntity>(_data);

        public IReadOnlyDictionary<uint, CharacterBackgroundTableEntity> ReadCharacterBackgroundTable() =>
            Read<uint, CharacterBackgroundTableEntity>(_data);

        public IReadOnlyDictionary<ushort, DistrictTableEntity> ReadDistrictTable() =>
            Read<ushort, DistrictTableEntity>(_data);

        public IReadOnlyDictionary<uint, ItemTableEntity> ReadItemTable() =>
            Read<uint, ItemTableEntity>(_data);

        public IReadOnlyDictionary<uint, ItemClassifyTableEntity> ReadItemClassifyTable() =>
            Read<uint, ItemClassifyTableEntity>(_data);

        public IReadOnlyDictionary<uint, ItemScriptTableEntity> ReadItemScriptTable() =>
            Read<uint, ItemScriptTableEntity>(_data);

        public IReadOnlyDictionary<ushort, CharacterInfoTableEntity> ReadCharacterInfoTable() =>
            Read<ushort, CharacterInfoTableEntity>(_data);

        public IReadOnlyDictionary<uint, PhotoItemTableEntity> ReadPhotoItemTable() =>
            Read<uint, PhotoItemTableEntity>(_data);

        public IReadOnlyDictionary<ushort, GestureTableEntity> ReadGestureTable() =>
            Read<ushort, GestureTableEntity>(_data);

        public IReadOnlyDictionary<ushort, MazeInfoTableEntity> ReadMazeInfoTable() =>
            Read<ushort, MazeInfoTableEntity>(_data);

        public IReadOnlyDictionary<ushort, BoosterTableEntity> ReadBoosterTable() =>
            Read<ushort, BoosterTableEntity>(_data);

        public IReadOnlyDictionary<uint, NpcTableEntity> ReadNpcTable() =>
            Read<uint, NpcTableEntity>(_data);

        public IReadOnlyDictionary<uint, PassInfoTableEntity> ReadPassInfoTable() =>
            Read<uint, PassInfoTableEntity>(_data);

        public IReadOnlyDictionary<uint, PassRewardInfoTableEntity> ReadPassRewardInfoTable() =>
            Read<uint, PassRewardInfoTableEntity>(_data);

        public BinTable(IConfiguration configuration) => _data = new(configuration);

        internal static IReadOnlyDictionary<TId, TItem> Read<TId, TItem>(BinFile data) where TId : IConvertible where TItem : ITableEntity<TId> => GetEntries(data, typeof(TItem).GetCustomAttribute<BinTableAttribute>()!.Name)
            .Select(br => (TItem)(Activator.CreateInstance(typeof(TItem), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { br }, null) ?? throw new ApplicationException()))
            .ToDictionary(k => k.Id, v => v);

        private static IEnumerable<BinaryReader> GetEntries(BinFile data, string file)
        {
            using BinaryReader br = new(data.GetInputStream(data.GetEntry($"../bin/Table/{file}.res")));

            for (uint q = 0, count = br.ReadUInt32(); q < count; ++q)
                yield return br;
        }

        void IDisposable.Dispose()
        {
            ((IDisposable)_data).Dispose();

            GC.SuppressFinalize(this);
        }
    }
}

// https://youtu.be/kOsodyjYfG8
