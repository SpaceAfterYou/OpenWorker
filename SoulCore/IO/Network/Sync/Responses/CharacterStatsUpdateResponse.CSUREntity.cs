using SoulCore.Game.Enums;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed partial record CharacterStatsUpdateResponse
    {
        public sealed record CSUREntity
        {
            public CharacterStat Id { get; init; }
            public float Value { get; init; }
        }
    }
}
