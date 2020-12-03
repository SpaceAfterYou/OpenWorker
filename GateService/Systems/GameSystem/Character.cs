using Core.DatabaseSystem.Characters;

namespace GateService.Systems.GameSystem
{
    public sealed class Character
    {
        public uint Id { get; init; }
        public byte SlotId { get; init; }

        public Character(CharacterModel model)
        {
            Id = model.Id;
            SlotId = model.SlotId;
        }
    }
}