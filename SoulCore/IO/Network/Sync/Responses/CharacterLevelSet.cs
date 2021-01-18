namespace SoulCore.IO.Network.Sync.Responses
{
    public sealed record CharacterLevelSet
    {
        public int Character { get; init; }
        public byte Level { get; init; }
    }
}
