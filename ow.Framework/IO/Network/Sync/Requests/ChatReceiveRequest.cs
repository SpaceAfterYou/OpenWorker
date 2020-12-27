using ow.Framework.Extensions;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Attributes;
using System.IO;

namespace ow.Framework.IO.Network.Sync.Requests
{
    [Request]
    public sealed record ChatReceiveRequest
    {
        public ChatType Type { get; }
        public string Message { get; }

        public ChatReceiveRequest(BinaryReader br) => (Type, Message) = (br.ReadChatType(), br.ReadNumberLengthUnicodeString());
    }
}
