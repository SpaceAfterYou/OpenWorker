using ow.Framework.Game.Character;
using ow.Framework.Game.Datas;
using ow.Framework.Game.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Service.District.Game;
using System.Linq;

namespace ow.Service.District.Network
{
    public static class GameSessionExtension
    {
        #region Send Characters

        internal static GameSession SendCharacterToggleWeapon(this GameSession session, in ToggleWeaponRequest request)
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

            foreach (CachedNpc npc in npcs)
            {
                writer.Write(npc.Id);
                writer.WriteVector3(npc.Position);
                writer.Write(npc.Rotation);
                writer.Write(uint.MinValue);
                writer.Write(npc.Waypoint);
                writer.Write(uint.MinValue);
                writer.WriteNpcVisability(NpcVisablity.Visible);
                writer.Write(npc.MobId);
            }

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterInfo(this GameSession session)
        {
            return session;
        }

        internal static GameSession SendCharacterStatsUpdate(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterStatsUpdate);

            writer.Write((byte)0);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            IStatsEntity stats = session.Entity.Get<IStatsEntity>();
            writer.Write((byte)stats.Count);

            foreach (StatEntity stat in stats)
            {
                writer.Write(stat.Value);
                writer.Write((ushort)stat.Id);
            }

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterProfileInfo(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterProfileInfo);

            ProfileEntity profile = session.Entity.Get<ProfileEntity>();

            writer.WriteProfileStatus(profile.Status);
            writer.WriteByteLengthUnicodeString(profile.About);
            writer.WriteByteLengthUnicodeString(profile.Note);

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterGestureLoad(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.GestureLoad);

            Gestures gestures = session.Entity.Get<Gestures>();

            foreach (uint gesture in gestures)
                writer.Write(gesture);

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterPostInfo(this GameSession session)
        {
            return session;
        }

        internal static GameSession SendCharacterOtherInfos(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterOtherInfos);

            DimensionEntity dimension = session.Entity.Get<DimensionEntity>();

            /// (.Values) will make copy all sessions in channel
            GameSession[] sessions = dimension.Sessions.Values.ToArray();

            writer.Write((short)sessions.Length);
            foreach (GameSession member in sessions)
            {
                writer.WriteCharacter(member);

                Place place = member.Entity.Get<Place>();
                writer.WritePlace(place);
            }

            return session.SendAsync(writer);
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