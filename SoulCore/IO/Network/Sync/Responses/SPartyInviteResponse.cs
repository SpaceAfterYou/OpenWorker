namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record SPartyInviteResponse
    {
        public readonly struct Character
        {
            public int Id { get; init; }
            public string Name { get; init; }
        }

        public Character Master { get; init; }
        public Character Member { get; init; }
    }
}
