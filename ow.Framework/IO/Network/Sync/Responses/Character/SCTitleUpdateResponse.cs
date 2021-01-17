namespace ow.Framework.IO.Network.Sync.Responses.Character
{
    public sealed partial record SCTitleUpdateResponse
    {
        public Entity Inside { get; init; }
        public Entity Outside { get; init; }
    }
}