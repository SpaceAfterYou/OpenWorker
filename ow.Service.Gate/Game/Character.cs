using ow.Framework.Database.Characters;
using ow.Framework.Game.Character;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using ow.Framework.Game.Storage;
using System.Linq;

namespace ow.Service.Gate.Game
{
    internal sealed class Character : ICharacter
    {
        public int Id { get; }
        public byte Slot { get; }
        public byte Level { get; }
        public Hero Hero { get; }
        public byte Advancement { get; }
        public IPhotoItemTableEntity Photo { get; }
        public ICharacterAppearance Appearance { get; }
        public string Name { get; }
        public IStorage Storage { get; }
        public Place Place { get; }
        public ICharacterStats Stats { get; }

        internal Character(CharacterModel model, BinTables binTable)
        {
            Id = model.Id;
            Slot = model.SlotId;
            Level = model.Level;
            Hero = model.Hero;
            Advancement = model.Advancement;

            if (binTable.PhotoItemTable.TryGetValue(model.PhotoId, out IPhotoItemTableEntity entity))
                Photo = entity;
            else
                Photo = binTable.PhotoItemTable.Values.First(c => c.Hero == Hero && c.Unknown14 == 1);

            Appearance = new Appearance(model.Appearance);
            Name = model.Name;
            Storage = new Storage(model);
            Place = new(model.Place);
        }
    }
}