namespace ow.Framework.Game.Character
{
    public interface ICharacterAppearance
    {
        ICharacterHair Hair { get; }
        ushort EyeColor { get; }
        ushort SkinColor { get; }
        ICharacterHair EquippedHair { get; }
        ushort EquippedEyeColor { get; }
        ushort EquippedSkinColor { get; }
    }
}