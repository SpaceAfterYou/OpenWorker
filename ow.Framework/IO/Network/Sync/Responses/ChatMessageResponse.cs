using ow.Framework.Game.Enums;

namespace ow.Framework.IO.Network.Sync.Responses
{
    public sealed record ChatMessageResponse
    {
        public int Character { get; init; }
        public ChatType Chat { get; init; }
        public string Message { get; init; } = default!;
    }
}