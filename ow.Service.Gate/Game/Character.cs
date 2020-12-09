using ow.Framework.Database.Characters;
using ow.Framework.Game.Ids;

namespace ow.Service.Gate.Game
{
    public sealed class Character
    {
        public uint Id { get; init; }
        public byte SlotId { get; init; }
        public byte Level { get; init; }
        public HeroId Hero { get; init; }
        public byte Advancement { get; init; }
        public uint PortraitId { get; init; }
        public Appearance Appearance { get; init; }
        public string Name { get; init; }
        public Storage Storage { get; init; }
        public Place Place { get; init; }

        public Character(CharacterModel model)
        {
            Id = model.Id;
            SlotId = model.SlotId;
            Level = model.Level;
            Hero = model.Hero;
            Advancement = model.Advancement;
            PortraitId = model.PortraitId;
            Appearance = new(model.Appearance);
            Name = model.Name;
            Storage = new(model);
            Place = new(model.Place);
        }
    }
}