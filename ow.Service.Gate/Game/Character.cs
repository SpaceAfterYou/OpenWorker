using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;

namespace ow.Service.Gate.Game
{
    internal sealed class Character
    {
        internal Storage Storage { get; }
        internal EntityCharacter Entity { get; }

        internal Character(CharacterModel model, BinTables tables)
        {
            Storage = new(model);
            Entity = new(model, tables);
        }
    }
}