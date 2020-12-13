using Microsoft.Extensions.Logging;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Service.District.Game;

namespace ow.Service.District.Network
{
    public sealed class Session : GameSession
    {
        public Channel Channel { get; set; }
        public Character Character { get; init; }
        public Profile Profile { get; init; }

        public Session(Server server, HandlerProvider provider, ILogger logger) : base(server, provider, logger)
        {
        }

        #region Send Characters

        internal Session SendNpcOtherInfos(IReadOnlyCachedNpcs npcs)
        {
            using PacketWriter writer = new(ClientOpcode.NpcOtherInfos);

            writer.Write((ushort)npcs.Count);

            uint serverId = 0;
            foreach (IReadOnlyCachedNpc npc in npcs)
            {
                writer.Write(serverId++);
                writer.WriteVector3(npc.Position);
                writer.Write(npc.Rotation);
                writer.Write(uint.MinValue);
                writer.Write(npc.Waypoint);
                writer.Write(uint.MinValue);
                writer.WriteNpcVisability(NpcVisablity.Visible);
                writer.Write(npc.Id);
            }

            return (Session)SendAsync(writer);
        }

        internal Session SendCharacterOtherInfos()
        {
            using PacketWriter writer = new(ClientOpcode.CharacterOtherInfos);

            return (Session)SendAsync(writer);
        }

        #endregion Send Characters

        #region Send Chat

        internal Session SendChatMessage(in ReceiveRequest request) =>
            SendChatMessage(request.Type, request.Message);

        internal Session SendChatMessage(ChatType type, string message)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            writer.Write(Character.Id);
            writer.WriteChatType(type);
            writer.WriteByteLengthUnicodeString(message);

            return (Session)SendAsync(writer);
        }

        #endregion Send Chat
    }
}