using ow.Framework.Database.Characters;

namespace ow.Framework.Game.Character
{
    public sealed class AppearanceCharacter
    {
        public HairCharacter Hair { get; }
        public ushort EyeColor { get; }
        public ushort SkinColor { get; }
        public HairCharacter EquippedHair { get; }
        public ushort EquippedEyeColor { get; }
        public ushort EquippedSkinColor { get; }

        public AppearanceCharacter(AppearanceModel model)
        {
            Hair = new(model.Hair);
            EyeColor = model.EyeColor;
            SkinColor = model.SkinColor;
            EquippedHair = new(model.EquippedHair);
            EquippedEyeColor = model.EquippedEyeColor;
            EquippedSkinColor = model.EquippedSkinColor;
        }
    }
}
