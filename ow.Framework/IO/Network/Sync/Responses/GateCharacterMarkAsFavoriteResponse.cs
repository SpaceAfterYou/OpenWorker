namespace ow.Framework.IO.Network.Responses
{
    public sealed record GateCharacterMarkAsFavoriteResponse
    {
        public int AccountId { get; init; }
        public int CharacterId { get; init; }
        public string CharacterName { get; init; } = default!;
        public uint PhotoId { get; init; }
    }
}