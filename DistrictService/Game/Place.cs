using ow.Framework.Database.Characters;
using System.Numerics;

namespace DistrictService.Game
{
    public class Place
    {
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }

        public Place(PlaceModel model)
        {
            Position = new(model.Position.X, model.Position.Y, model.Position.Z);
            Rotation = model.Rotation;
        }
    }
}
