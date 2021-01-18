namespace SoulCore.IO.Network.Sync.Responses.Character
{
    public sealed partial record SCTitleUpdateResponse
    {
        public readonly struct Entity
        {
            public uint Prefix { get; init; }
            public uint Suffix { get; init; }
        }
    }
}
