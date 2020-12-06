using Core.Systems.DatabaseSystem.Characters;

namespace DistrictService.Systems.GameSystem
{
    public class Character
    {
        public uint Id { get; init; }
        public byte Level { get; set; }
        public Place Place { get; init; }

        public Character(CharacterModel model)
        {
            Id = model.Id;
            Level = model.Level;
            Place = new(model.Place);
        }
    }
}