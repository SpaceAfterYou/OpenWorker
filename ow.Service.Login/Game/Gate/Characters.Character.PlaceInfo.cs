using ow.Framework.Database.Characters;
using System.Numerics;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal readonly struct PlaceInfo
            {
                internal Vector3 Postion { get; init; }
                internal float Rotation { get; init; }
                internal ushort District { get; init; }

                internal PlaceInfo(PlaceModel model)
                {
                    Postion = new Vector3(model.Position.X, model.Position.Y, model.Position.Z);
                    Rotation = model.Rotation;
                    District = model.Location;
                }
            }
        }
    }
}