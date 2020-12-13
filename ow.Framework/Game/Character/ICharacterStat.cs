using ow.Framework.Game.Enums;

namespace ow.Framework.Game.Character
{
    public interface ICharacterStat
    {
        Stat Id { get; }
        float Value { get; }
    }
}