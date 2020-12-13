using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;

namespace ow.Framework.Game
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
    }
}