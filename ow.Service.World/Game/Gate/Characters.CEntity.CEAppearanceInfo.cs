namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters
    {
        internal sealed partial class CEntity
        {
            internal readonly struct CEAppearanceInfo
            {
                internal readonly struct CEAIHairInfo
                {
                    internal ushort Style { get; init; }
                    internal ushort Color { get; init; }
                }

                internal CEAIHairInfo Hair { get; init; }
                internal ushort EyesColor { get; init; }
                internal ushort SkinColor { get; init; }
                internal CEAIHairInfo EquippedHair { get; init; }
                internal ushort EquippedEyesColor { get; init; }
                internal ushort EquippedSkinColor { get; init; }
            }
        }
    }
}