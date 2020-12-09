using DistrictService.Game;
using Microsoft.Extensions.Logging;
using ow.Framework.Game.Types;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using ow.Framework.IO.Network.Requests.Chat;

namespace DistrictService.Network
{
    public sealed class Session : SwSession
    {
        public Channel Channel { get; set; }
        public Character Character { get; init; }
        public Profile Profile { get; init; }

        public Session(Server server, HandlerProvider provider, ILogger logger) : base(server, provider, logger)
        {
        }

        #region Send Chat

        internal Session SendChatMessage(in ReceiveRequest request) =>
            SendChatMessage(request.Type, request.Message);

        internal Session SendChatMessage(ChatType type, string message)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            writer.Write(Character.Id);
            writer.WriteChatType(type);
            writer.WriteByteLengthUnicodeString(message);

            return SendAsync(writer) as Session;
        }

        #endregion Send Chat
    }
}