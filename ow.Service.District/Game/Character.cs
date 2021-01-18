using SoulCore.Database.Characters;
using SoulCore.Game.Enums;
using System.Numerics;

namespace ow.Service.District.Game
{
    public sealed class Character
    {
        public sealed class AppearanceInfo
        {
            public sealed class HairInfo
            {
                public ushort Style { get; set; }
                public ushort Color { get; set; }

                public HairInfo(HairModel model) => (Style, Color) = (model.Style, model.Color);
            }

            public HairInfo Hair { get; }
            public ushort EyeColor { get; set; }
            public ushort SkinColor { get; set; }
            public HairInfo EquippedHair { get; }
            public ushort EquippedEyeColor { get; set; }
            public ushort EquippedSkinColor { get; set; }

            public AppearanceInfo(AppearanceModel model)
            {
                Hair = new(model.Hair);
                EyeColor = model.EyeColor;
                SkinColor = model.SkinColor;
                EquippedHair = new(model.EquippedHair);
                EquippedEyeColor = model.EquippedEyeColor;
                EquippedSkinColor = model.EquippedSkinColor;
            }
        }

        public sealed class PlaceInfo
        {
            public Vector3 Position { get; set; }
            public float Rotation { get; set; }

            public PlaceInfo(PlaceModel model) => (Position, Rotation) = (new(model.Position.X, model.Position.Y, model.Position.Z), model.Rotation);
        }

        public int Id { get; }
        public byte Level { get; set; }
        public Hero Hero { get; }
        public CharacterAdvancement Advancement { get; set; }
        public uint Photo { get; }
        public string Name { get; set; }
        public ulong Exp { get; set; }
        public PlaceInfo Place { get; init; }
        public AppearanceInfo Appearance { get; init; }

        public Character(CharacterModel model)
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
