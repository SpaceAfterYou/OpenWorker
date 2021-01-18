using SoulCore.Extensions;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Sync.Attributes;
using System.IO;

namespace SoulCore.IO.Network.Sync.Requests
{
    [Request]
    public sealed record ChatReceiveRequest
    {
        public ChatType Type { get; }
        public string Message { get; }

        public ChatReceiveRequest(BinaryReader br) => (Type, Message) = (br.ReadChatType(), br.ReadNumberLengthUnicodeString());
    }
}
