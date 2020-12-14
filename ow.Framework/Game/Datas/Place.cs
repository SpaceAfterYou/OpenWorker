using ow.Framework.Database.Characters;
using System.Numerics;

namespace ow.Framework.Game.Datas
{
    public sealed class Place
    {
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public ushort Location { get; }

        public Place(PlaceModel model)
        {
            Location = model.Location;
            Position = new(model.Position.X, model.Position.Y, model.Position.Z);
            Rotation = model.Rotation;
        }
    }
}