using ow.Framework.Game.Character;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Service.District.Game;

namespace ow.Service.District.Network
{
    public static class GameSessionExtension
    {
        #region Send Characters

        internal static GameSession SendToggleWeapon(this GameSession session, in ToggleWeaponRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterToggleWeapon);

            writer.Write(request.CharacterId);
            writer.WriteVector3(request.Position);
            writer.Write(request.Rotation);
            writer.Write(request.Toggle);
            writer.Write(request.Unknown1);

            return session.SendAsync(writer);
        }

        internal static GameSession SendNpcOtherInfos(this GameSession session, CachedNpcs npcs)
        {
            using PacketWriter writer = new(ClientOpcode.NpcOtherInfos);

            writer.Write((ushort)npcs.Count);

            uint serverId = 0;
            foreach (CachedNpc npc in npcs)
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

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterOtherInfos(this GameSession session)
        {
            return session;

            //using PacketWriter writer = new(ClientOpcode.CharacterOtherInfos);

            //Dimension dimension = session.Entity.Get<Dimension>();

            //writer.Write((short)channel.Sessions.Count);
            //foreach (var s in channel.Sessions.Values)
            //{
            //    var character = s.GetComponent<Character>();
            //    writer.WriteMainData(character);

            //    var equipped = s.GetComponent<EquippedStorage>();
            //    writer.WriteWeaponData(equipped);
            //    writer.WriteFashionData(equipped);

            //    var stats = s.GetComponent<Stats>();
            //    writer.WriteMetaData(character, stats);
            //    writer.Write(character.WorldPosition);
            //}

            //return session.SendAsync(writer);
        }

        #endregion Send Characters

        #region Send Chat

        internal static GameSession SendChatMessage(this GameSession session, in ReceiveRequest request) =>
            session.SendChatMessage(request.Type, request.Message);

        internal static GameSession SendChatMessage(this GameSession session, ChatType type, string message)
        {
            using PacketWriter writer = new(ClientOpcode.ChatMessage);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.WriteChatType(type);
            writer.WriteByteLengthUnicodeString(message);

            return session.SendAsync(writer);
        }

        #endregion Send Chat
    }
}