using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Extensions;
using SoulWorker.Extensions;
using SoulWorker.Types;
using System.IO;

namespace Core.Systems.NetSystem.Requests.Chat
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
