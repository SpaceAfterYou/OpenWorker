using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using ow.Framework.Game.Storage;

namespace ow.Framework.Game.Character
{
    public interface ICharacter
    {
        int Id { get; }
        byte Slot { get; }
        byte Level { get; }
        Hero Hero { get; }
        byte Advancement { get; }
        public IPhotoItemTableEntity Photo { get; }
        string Name { get; }
        ICharacterAppearance Appearance { get; }
        IStorage Storage { get; }
        ICharacterStats Stats { get; }
    }
}