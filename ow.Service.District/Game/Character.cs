using ow.Framework.Database.Characters;
using ow.Framework.Game.Enums;
using System.Numerics;

namespace ow.Service.District.Game
{
    internal sealed class Character
    {
        internal sealed class AppearanceInfo
        {
            internal sealed class HairInfo
            {
                internal ushort Style { get; set; }
                internal ushort Color { get; set; }

                internal HairInfo(HairModel model) => (Style, Color) = (model.Style, model.Color);
            }

            internal HairInfo Hair { get; }
            internal ushort EyeColor { get; set; }
            internal ushort SkinColor { get; set; }
            internal HairInfo EquippedHair { get; }
            internal ushort EquippedEyeColor { get; set; }
            internal ushort EquippedSkinColor { get; set; }

            internal AppearanceInfo(AppearanceModel model)
            {
                Hair = new(model.Hair);
                EyeColor = model.EyeColor;
                SkinColor = model.SkinColor;
                EquippedHair = new(model.EquippedHair);
                EquippedEyeColor = model.EquippedEyeColor;
                EquippedSkinColor = model.EquippedSkinColor;
            }
        }

        internal sealed class PlaceInfo
        {
            public Vector3 Position { get; set; }
            public float Rotation { get; set; }

            public PlaceInfo(PlaceModel model) => (Position, Rotation) = (new(model.Position.X, model.Position.Y, model.Position.Z), model.Rotation);
        }

        internal int Id { get; }
        internal byte Level { get; set; }
        internal Hero Hero { get; }
        internal CharacterAdvancement Advancement { get; set; }
        internal uint Photo { get; }
        internal string Name { get; set; }
        internal ulong Exp { get; set; }
        internal PlaceInfo Place { get; init; }
        internal AppearanceInfo Appearance { get; init; }

        internal Character(CharacterModel model)
        {
            Id = model.Id;
            Level = model.Level;
            Hero = model.Hero;
            Advancement = model.Advancement;
            Name = model.Name;
            Exp = model.Exp;
            Photo = model.Photo;
            Place = new(model.Place);
            Appearance = new(model.Appearance);
        }
    }
}