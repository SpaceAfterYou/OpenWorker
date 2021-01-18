namespace SoulCore.Database.Characters
{
    public sealed class AppearanceModel
    {
        public HairModel Hair { get; set; } = default!;
        public ushort EyeColor { get; set; }
        public ushort SkinColor { get; set; }
        public HairModel EquippedHair { get; set; } = default!;
        public ushort EquippedEyeColor { get; set; }
        public ushort EquippedSkinColor { get; set; }
    }
}
