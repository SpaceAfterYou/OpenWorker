using Core.Systems.NetSystem;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using Core.Systems.NetSystem.Providers;
using Core.Systems.NetSystem.Requests.Chat;
using DistrictService.Systems.GameSystem;
using Microsoft.Extensions.Logging;
using SoulWorker.Types;

namespace DistrictService.Systems.NetSystem
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
            using WriterPacket writer = new(ClientOpcode.ChatMessage);

            writer.Write(Character.Id);
            writer.Write(type);
            writer.WriteByteLengthUnicodeString(message);

            return SendAsync(writer) as Session;
        }

        #endregion Send Chat
    }
}