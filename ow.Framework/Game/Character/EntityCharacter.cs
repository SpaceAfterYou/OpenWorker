using ow.Framework.Database.Characters;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.Game.Enums;
using System.Linq;

namespace ow.Framework.Game.Character
{
    public sealed class EntityCharacter
    {
        public int Id { get; }
        public byte Slot { get; }
        public byte Level { get; set; }
        public Hero Hero { get; }
        public byte Advancement { get; set; }
        public IPhotoItemTableEntity Photo { get; }
        public string Name { get; set; }
        public AppearanceCharacter Appearance { get; }

        public EntityCharacter(CharacterModel model, IBinTables tables)
        {
            Id = model.Id;
            Slot = model.SlotId;
            Level = model.Level;
            Hero = model.Hero;
            Advancement = model.Advancement;
            Photo = GetPhoto(model, tables);
            Appearance = new AppearanceCharacter(model.Appearance);
            Name = model.Name;
        }

        private static IPhotoItemTableEntity GetPhoto(CharacterModel model, IBinTables tables) =>
            tables.PhotoItemTable.TryGetValue(model.PhotoId, out IPhotoItemTableEntity entity)
                ? entity
                : tables.PhotoItemTable.Values.First(c => c.Hero == model.Hero && c.Unknown14 == 1);
    }
}