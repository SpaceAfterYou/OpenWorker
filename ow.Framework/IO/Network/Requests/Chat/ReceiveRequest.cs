using ow.Framework.IO.Network.Attributes;
using ow.Framework.Game.Types;
using ow.Framework.Extensions;
using System.IO;

namespace ow.Framework.IO.Network.Requests.Chat
{
    [Request]
    public readonly struct ReceiveRequest
    {
        public ChatType Type { get; }
        public string Message { get; }

        public ReceiveRequest(BinaryReader br) =>
            (Type, Message) = (br.ReadChatType(), br.ReadNumberLengthUnicodeString());
    }
}
