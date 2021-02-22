namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal readonly partial struct AppearanceInfo
            {
                internal readonly struct HairInfo
                {
                    internal ushort Style { get; init; }
                    internal ushort Color { get; init; }
                }
            }
        }
    }
}