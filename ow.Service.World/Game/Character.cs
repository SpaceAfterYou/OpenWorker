using ow.Framework.Database.Characters;
using SoulCore.Types;
using System.Numerics;

namespace ow.Service.District.Game
{
    internal sealed class Character
    {
        internal sealed class AppearanceInfo
        {
            internal sealed class HairInfo
            {
                internal readonly static HairInfo Empty = new();

                internal ushort Style { get; set; }
                internal ushort Color { get; set; }

                internal HairInfo(HairModel model) => (Style, Color) = (model.Style, model.Color);

                private HairInfo()
                {
                }
            }

            internal readonly static AppearanceInfo Empty = new();

            internal HairInfo Hair { get; } = HairInfo.Empty;
            internal ushort EyeColor { get; set; }
            internal ushort SkinColor { get; set; }
            internal HairInfo EquippedHair { get; } = HairInfo.Empty;
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

            private AppearanceInfo()
            {
            }
        }

        internal sealed class PlaceInfo
        {
            internal static PlaceInfo Empty = new();

            internal Vector3 Position { get; set; }
            internal float Yaw { get; set; }

            internal PlaceInfo(PlaceModel model)
            {
                Position = new(model.Position.X, model.Position.Y, model.Position.Z);
                Yaw = model.Rotation;
            }

            private PlaceInfo()
            {
            }
        }

        internal static readonly Character Empty = new();

        internal int Id { get; }
        internal byte Level { get; set; }
        internal Class Class { get; }
        internal ClassAdvancement Advancement { get; set; }
        internal uint Photo { get; }
        internal string Name { get; set; } = string.Empty;
        internal ulong Exp { get; set; }
        internal PlaceInfo Place { get; } = PlaceInfo.Empty;
        internal AppearanceInfo Appearance { get; } = AppearanceInfo.Empty;

        internal Character(CharacterModel model)
        {
            Id = model.Id;
            Level = model.Level;
            Class = model.Class;
            Advancement = model.Advancement;
            Name = model.Name;
            Exp = model.Exp;
            Photo = model.Photo;
            Place = new(model.Place);
            Appearance = new(model.Appearance);
        }

        private Character()
        {
        }
    }
}