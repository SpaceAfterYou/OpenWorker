namespace ow.Framework.Database.Characters
{
    public sealed class AppearanceModel
    {
        public HairModel Hair { get; set; }
        public ushort EyeColor { get; set; }
        public ushort SkinColor { get; set; }
        public HairModel EquippedHair { get; set; }
        public ushort EquippedEyeColor { get; set; }
        public ushort EquippedSkinColor { get; set; }
    }
}