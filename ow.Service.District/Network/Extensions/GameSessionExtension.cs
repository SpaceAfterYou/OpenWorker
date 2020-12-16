using ow.Framework.Game.Character;
using ow.Framework.Game.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Requests.Character;
using ow.Framework.IO.Network.Requests.Chat;
using ow.Framework.IO.Network.Requests.Server;
using ow.Service.District.Game;
using ow.Service.District.Game.Entities;
using ow.Service.District.Game.Enums;
using ow.Service.District.Game.Repositories;
using System;

namespace ow.Service.District.Network
{
    public static class GameSessionExtension
    {
        #region Send Characters

        internal static GameSession SendCharacterDbLoadSync(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterDbLoadSync);
            return session.SendAsync(writer);
        }

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

        internal static GameSession SendNpcOtherInfos(this GameSession session, CachedNpcRepository npcs)
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

            GesturesEntity gestures = session.Entity.Get<GesturesEntity>();

            foreach (uint gesture in gestures)
                writer.Write(gesture);

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterPostInfo(this GameSession session)
        {
            return session;
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

        #region Send Maze

        internal static GameSession SendMazeDayEventBoosters(this GameSession session, MazeDayEventBoosterRepository boosters)
        {
            using PacketWriter writer = new(ClientOpcode.EventDayEventBoosterList);

            writer.Write((ushort)boosters.Count);
            foreach (var booster in boosters)
            {
                writer.Write(booster.Maze.Id);
                writer.Write(booster.Id);
            }

            return session.SendAsync(writer);
        }

        #endregion Send Maze

        #region Send Service

        internal static GameSession SendServiceHeartbeat(this GameSession session, in HeartbeatRequest request)
        {
            using PacketWriter writer = new(ClientOpcode.Heartbeat);

            writer.Write(request.Tick);

            return session.SendAsync(writer);
        }

        internal static GameSession SendServiceLogOut(this GameSession session, GateInstance gate)
        {
            using PacketWriter writer = new(ClientOpcode.LogOut);

            AccountEntity account = session.Entity.Get<AccountEntity>();
            writer.Write(account.Id);

            EntityCharacter character = session.Entity.Get<EntityCharacter>();
            writer.Write(character.Id);

            writer.WriteNumberLengthUtf8String(gate.Ip);
            writer.Write(gate.Port);
            writer.WriteLogoutWay(LogoutWay.GateService);
            writer.WriteCanLogOutConnect(CanLogOutConnect.Yes);

            return session.SendAsync(writer);
        }

        #endregion Send Service

        #region Send World

        internal static GameSession SendWorldEnter(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.WorldEnter);

            writer.Write(uint.MinValue);
            writer.WriteCanWorldConnect(CanWorldConnect.Yes);

            PlaceEntity place = session.Entity.Get<PlaceEntity>();
            writer.WritePlace(place);

            writer.Write(byte.MinValue);

            return session.SendAsync(writer);
        }

        internal static GameSession SendWorldVersion(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.WorldVersion);

            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            return session.SendAsync(writer);
        }

        #endregion Send World

        #region Send Boosters

        internal static GameSession SendBoosterRemove(this GameSession session, byte id)
        {
            using PacketWriter writer = new(ClientOpcode.BoosterRemove);

            writer.Write(id);

            return session.SendAsync(writer);
        }

        internal static GameSession SenBoosterAdd(this GameSession session, BoosterRepository boosters)
        {
            byte id = 0; /// TODO: unique id? sequence id?
            foreach (Booster booster in boosters)
            {
                using PacketWriter writer = new(ClientOpcode.BoosterAdd);

                writer.Write(id++);
                writer.Write(booster.Prototype.Id);
                writer.Write(Math.Max((ulong)(booster.End - DateTimeOffset.Now).TotalSeconds, 0UL));

                session.SendAsync(writer);
            }

            return session;
        }

        #endregion Send Boosters
    }
}