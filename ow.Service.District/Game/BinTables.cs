using Microsoft.Extensions.Configuration;
using ow.Framework.FS.Datas.Bin.Table;
using ow.Framework.FS.Datas.Bin.Table.Entities;
using ow.Framework.IO.File.Bin;
using System.Collections.Generic;

namespace ow.Service.District.Game
{
    public sealed class BinTables : IBinTables
    {
        public IReadOnlyDictionary<ushort, DistrictTableEntity> DistrictTable { get; }
        public IReadOnlyDictionary<ushort, MazeInfoTableEntity> MazeInfoTable { get; }
        public IReadOnlyDictionary<ushort, BoosterTableEntity> BoosterTable { get; }
        public IReadOnlyDictionary<uint, PhotoItemTableEntity> PhotoItemTable { get; }

        public BinTables(IConfiguration configuration)
        {
            using BinTable tables = new(configuration);

            DistrictTable = tables.ReadDistrictTable();
            MazeInfoTable = tables.ReadMazeInfoTable();
            BoosterTable = tables.ReadBoosterTable();
            PhotoItemTable = tables.ReadPhotoItemTable();
        }
    }
}
