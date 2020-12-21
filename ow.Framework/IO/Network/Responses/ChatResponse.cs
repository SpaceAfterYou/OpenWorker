using ow.Framework.Game.Enums;

namespace ow.Framework.IO.Network.Responses
{
    public sealed record ChatResponse
    {
        public ChatType Chat { get; init; }
        public string Message { get; init; } = default!;
        public int Character { get; init; }
    }
}