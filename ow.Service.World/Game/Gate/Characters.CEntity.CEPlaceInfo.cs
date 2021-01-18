using SoulCore.Database.Characters;
using System.Numerics;

namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters
    {
        internal sealed partial class CEntity
        {
            internal readonly struct CEPlaceInfo
            {
                internal Vector3 Postion { get; init; }
                internal float Rotation { get; init; }
                internal ushort District { get; init; }

                internal CEPlaceInfo(PlaceModel model)
                {
                    Postion = new Vector3(model.Position.X, model.Position.Y, model.Position.Z);
                    Rotation = model.Rotation;
                    District = model.Location;
                }
            }
        }
    }
}
