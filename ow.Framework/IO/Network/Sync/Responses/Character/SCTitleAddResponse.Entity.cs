namespace ow.Framework.IO.Network.Sync.Responses.Character
{
    public sealed partial record SCTitleAddResponse
    {
        public readonly struct Entity
        {
            public uint Id { get; init; }
            public int LogType { get; init; }
        }
    }
}