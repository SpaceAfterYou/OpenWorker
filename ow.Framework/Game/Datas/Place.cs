using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using System.Numerics;

namespace ow.Framework.Game.Datas
{
    public sealed class Place
    {
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public DistrictTableEntity District { get; }

        public Place(PlaceModel model, IBinTables tables)
        {
            District = tables.DistrictTable[model.Location];
            Position = new(model.Position.X, model.Position.Y, model.Position.Z);
            Rotation = model.Rotation;
        }
    }
}