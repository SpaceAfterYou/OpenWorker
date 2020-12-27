using ow.Framework.Game.Types;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record CharacterProfileResponse
    {
        public ProfileStatus Status { get; init; }
        public string About { get; init; } = default!;
        public string Note { get; init; } = default!;
    }
}
