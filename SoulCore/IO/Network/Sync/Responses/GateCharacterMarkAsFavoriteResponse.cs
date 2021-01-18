using SoulCore.Game.Enums;

namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record GateCharacterMarkAsFavoriteResponse
    {
        public int AccountId { get; init; }
        public int CharacterId { get; init; }
        public Hero Hero { get; init; }
        public byte Level { get; init; }
        public string CharacterName { get; init; } = default!;
        public uint PhotoId { get; init; }
        public long Date { get; init; }
        public int Error { get; init; }
    }
}
