namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal readonly partial struct AppearanceInfo
            {
                internal HairInfo Hair { get; init; }
                internal ushort EyesColor { get; init; }
                internal ushort SkinColor { get; init; }
                internal HairInfo EquippedHair { get; init; }
                internal ushort EquippedEyesColor { get; init; }
                internal ushort EquippedSkinColor { get; init; }
            }
        }
    }
}