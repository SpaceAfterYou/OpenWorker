using ow.Framework.Game.Enums;

namespace ow.Framework.Game.Entities
{
    public sealed class SpecialOptionEntity
    {
        public SpecialOption Id { get; init; }
        public float Value { get; set; }

        public SpecialOptionEntity(SpecialOption id, float value) => (Id, Value) = (id, value);
    }
}