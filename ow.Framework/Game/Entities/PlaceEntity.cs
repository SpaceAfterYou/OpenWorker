using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using System.Numerics;

namespace ow.Framework.Game.Entities
{
    public sealed class PlaceEntity
    {
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public DistrictTableEntity District { get; }

        public PlaceEntity(PlaceModel model, IBinTables tables)
        {
            District = tables.DistrictTable[model.Location];
            Position = new(model.Position.X, model.Position.Y, model.Position.Z);
            Rotation = model.Rotation;
        }
    }
}