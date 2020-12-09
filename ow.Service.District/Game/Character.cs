using ow.Framework.Database.Characters;

namespace ow.Service.District.Game
{
    public class Character
    {
        public int Id { get; init; }
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