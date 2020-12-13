using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;

namespace ow.Service.Gate.Game
{
    public sealed class Appearance : AppearanceCharacter
    {
        HairCharacter AppearanceCharacter.Hair => Hair;
        HairCharacter AppearanceCharacter.EquippedHair => Hair;

        public Hair Hair { get; init; }
        public ushort EyeColor { get; init; }
        public ushort SkinColor { get; init; }
        public Hair EquippedHair { get; init; }
        public ushort EquippedEyeColor { get; init; }
        public ushort EquippedSkinColor { get; init; }

        public Appearance(AppearanceModel model)
        {
            Hair = new Hair(model.Hair);
            EyeColor = model.EyeColor;
            SkinColor = model.SkinColor;
            EquippedHair = new Hair(model.EquippedHair);
            EquippedEyeColor = model.EquippedEyeColor;
            EquippedSkinColor = model.EquippedSkinColor;
        }
    }
}