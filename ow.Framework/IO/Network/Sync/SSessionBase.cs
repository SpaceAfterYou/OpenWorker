using Microsoft.Extensions.Logging;
using NetCoreServer;
using ow.Framework.Game;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Commands;
using ow.Framework.IO.Network.Sync.Commands.Old;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync.Responses.Shared;
using ow.Framework.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using static ow.Framework.IO.Network.Sync.Providers.HandlerProvider;
using static ow.Framework.IO.Network.Sync.Responses.SLUserCharacterForServerResponse;

namespace ow.Framework.IO.Network.Sync
{
    public abstract partial class SSessionBase : TcpSession
    {
        //public SSessionBase SendAsync(BattlePassLoadResponse value) =>
        //    SendAsync(SCCategory.InfiniteTower, SCInfiniteTower.LoadInfo, (SPacketWriter writer) =>
        //    {
        //        writer.Write(value.Id);
        //        writer.Write(ushort.MinValue);
        //        writer.Write(value.NextReward);
        //        writer.Write(ulong.MinValue);
        //        writer.Write(ulong.MinValue);
        //        writer.Write(value.HavePoint);
        //        writer.Write(byte.MinValue);
        //    });

        public SSessionBase SendDeferred(BattlePassLoadResponse value) =>
            SendDeferred(SCCategory.Event, SCEvent.BattlePassLoad, (SPacketWriter writer) =>
            {
                writer.Write(value.Id);
                writer.Write(value.NextReward);
                writer.Write(value.StartDate);
                writer.Write(value.EndDate);
                writer.Write(value.HavePoint);
                writer.Write(value.IsPremium);
            });

        public SSessionBase SendDeferred(InfiniteTowerLoadInfoResponse value) =>
            SendDeferred(SCCategory.InfiniteTower, SCInfiniteTower.LoadInfo, (SPacketWriter writer) =>
            {
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            });

        public SSessionBase SendDeferred(CharacterSkillInfoResponse value) =>
            SendDeferred(SCCategory.Skill, SCSkill.SkillLoadInfo, (SPacketWriter writer) =>
            {
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x01, 0x00, 0x08, 0x00, 0x08, 0x00, 0x03, 0x00, 0x0E, 0x00, 0x3B, 0xB9, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x1C, 0x0D, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0xEF, 0x14, 0xEF, 0x03, 0x00, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB8, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0xCB, 0xA7, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0xFB, 0x1C, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x53, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x2B, 0x92, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0xAF, 0x3A, 0xA5, 0x03, 0x00, 0x00, 0x00, 0x00, 0xF1, 0x0B, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x1A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1B, 0x6B, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x44, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x3B, 0xB9, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0xB7, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xCB, 0xA7, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0xFB, 0x1C, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x53, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2B, 0x92, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0xAF, 0x3A, 0xA5, 0x03, 0x00, 0x00, 0x00, 0x00, 0xEF, 0x0B, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x1B, 0x6B, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x44, 0xB3, 0x03, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0xF1, 0x0B, 0xB2, 0x03, 0x53, 0x0C, 0xB2, 0x03, 0xB8, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x53, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0xB8, 0x0C, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x1C, 0x0D, 0xB2, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x0B, 0x00, 0x15, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x01, 0x00, 0x0B, 0x00, 0x15, 0x00, 0x00, 0x00 });
            });

        public SSessionBase SendDeferred(ChannelInfoResponse value) =>
            SendDeferred(SCCategory.Channel, SCChannel.Info, (SPacketWriter writer) =>
            {
                writer.Write(value.Location);
                writer.Write((byte)value.Values.Count());
                foreach (ChannelInfoResponse.Entity channel in value.Values)
                {
                    writer.Write(channel.Id);
                    writer.WriteChannelLoadStatus(channel.Status);
                }
            });

        public SSessionBase SendDeferred(PostInfoResponse value) =>
            SendDeferred(SCCategory.Post, SCPost.Info, (SPacketWriter writer) =>
            {
                writer.Write(ushort.MinValue);
                writer.Write(value.Count);
            });

        public SSessionBase SendDeferred(SPartyInviteResponse value) =>
            SendDeferred(SCCategory.Party, SCParty.Invite, (SPacketWriter writer) =>
            {
                writer.WriteByteLengthUnicodeString(value.Member.Name);
                writer.WriteByteLengthUnicodeString(value.Master.Name);
                writer.Write(value.Master.Id);
                writer.Write(value.Member.Id);
                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
                writer.Write(uint.MinValue);
                writer.Write(byte.MinValue);
            });

        public SSessionBase SendDeferred(PartyDeleteResponse value) =>
            SendDeferred(SCCategory.Party, SCParty.Delete, (SPacketWriter writer) =>
            {
                writer.Write(value.Id);
            });

        public SSessionBase SendCharacterDbLoadDeferred() =>
            SendDeferred(SCCategory.Character, SCCharacter.DbLoadSync);

        public SSessionBase SendDeferred(NpcOthersInfosResponse value) =>
            SendDeferred(SCCategory.World, SCWorld.OtherInfosNpc, (SPacketWriter writer) =>
            {
                writer.Write((ushort)value.Values.Count());
                foreach (NpcOthersInfosResponse.Entity entity in value.Values)
                {
                    writer.Write(entity.Id);
                    writer.Write(entity.Position);
                    writer.Write(entity.Rotation);
                    writer.Write(uint.MinValue);
                    writer.Write(entity.Waypoint);
                    writer.Write(uint.MinValue);
                    writer.WriteNpcVisability(NpcVisablity.Visible);
                    writer.Write(entity.CreatureId);
                }
            });

        public SSessionBase SendDeferred(CharacterInfoResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.InfoRes, (SPacketWriter writer) =>
            {
                writer.WriteCharacter(value.Character);
                writer.WritePlace(value.Place);

                writer.Write(ulong.MinValue); // Exp
                writer.Write(ulong.MinValue); // Zenny

                writer.Write(uint.MinValue); // 1
                writer.Write(uint.MinValue); // 2
                writer.Write(uint.MinValue); // 3
                writer.Write(uint.MinValue); // 4
                writer.Write(uint.MinValue); // 5

                writer.Write(ulong.MinValue); // Aether
                writer.Write(ulong.MinValue);

                byte[] hz = new byte[9] { (byte)'1', (byte)'3', (byte)'4', (byte)'0', (byte)'0', (byte)'6', (byte)'8', (byte)'9', (byte)'3' }; // maybe privacy
                writer.Write((ushort)hz.Length);
                writer.Write(hz);

                writer.Write(ulong.MinValue);
                writer.Write(ulong.MinValue);
                writer.Write(uint.MinValue);
                writer.Write(ushort.MinValue);

                writer.Write(byte.MinValue);
                writer.WriteCharacterInfoResult(value.Result);
            });

        public SSessionBase SendDeferred(CharacterStatsUpdateResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.UpdateStatList, (SPacketWriter writer) =>
            {
                writer.Write((byte)0);

                writer.Write(value.Character);
                writer.Write((byte)value.Values.Count());

                foreach (CharacterStatsUpdateResponse.CSUREntity stat in value.Values)
                {
                    writer.Write(stat.Value);
                    writer.WriteCharacterStat(stat.Id);
                }
            });

        public SSessionBase SendDeferred(CharacterProfileResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.Community, (SPacketWriter writer) =>
            {
                writer.WriteProfileStatus(value.Status);
                writer.WriteByteLengthUnicodeString(value.About);
                writer.WriteByteLengthUnicodeString(value.Note);
            });

        public SSessionBase SendDeferred(CharacterPostInfoResponse value) =>
            SendDeferred(SCCategory.Post, SCPost.Info, (SPacketWriter writer) =>
            {
                writer.Write(ushort.MinValue);
                writer.Write((ushort)value.Values.Count());
            });

        public SSessionBase SendDeferred(CharacterGestureLoadResponse value) =>
            SendDeferred(SCCategory.Gesture, SCGesture.SlotLoad, (SPacketWriter writer) =>
            {
                foreach (uint gesture in value.Values)
                    writer.Write(gesture);
            });

        public SSessionBase SendDeferred(ChatMessageResponse value) =>
            SendDeferred(SCCategory.Chat, SCChat.Normal, (SPacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.WriteChatType(value.Chat);
                writer.WriteByteLengthUnicodeString(value.Message);
            });

        public SSessionBase SendDeferred(DayEventBoosterResponse value) =>
            SendDeferred(SCCategory.Event, SCEvent.DayEventBoosterList, (SPacketWriter writer) =>
            {
                writer.Write((ushort)value.Values.Count);
                foreach (DayEventBoosterResponse.Entity booster in value.Values)
                {
                    writer.Write(booster.Maze);
                    writer.Write(booster.Id);
                }
            });

        public SSessionBase SendDeferred(ServiceHeartbeatRequest value) =>
            SendDeferred(SCCategory.System, SCSystem.Ping, (SPacketWriter writer) =>
            {
                writer.Write(value.Tick);
            });

        public SSessionBase SendDeferred(DistrictLogOutResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.GobackLobby, (SPacketWriter writer) =>
            {
                writer.Write(value.Account);
                writer.Write(value.Character);
                writer.WriteNumberLengthUtf8String(value.Ip);
                writer.Write(value.Port);
                writer.WriteDistrictLogOutWay(DistrictLogOutWay.GoToGateService);
                writer.WriteDistrictLogOutStatus(DistrictLogOutStatus.Success);
            });

        public SSessionBase SendDeferred(DistrictEnterResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.EnterGameServerRes, (SPacketWriter writer) =>
            {
                writer.Write(uint.MinValue);
                writer.WriteDistrictConnectResult(value.Result);
                writer.WritePlace(value.Place);
                writer.Write(byte.MinValue);
            });

        public SSessionBase SendDeferred(WorldVersionResponse value) =>
            SendDeferred(SCCategory.World, SCWorld.Version, (SPacketWriter writer) =>
            {
                writer.Write(value.Id);
                writer.Write(value.Main);
                writer.Write(value.Sub);
                writer.Write(value.Data);
            });

        public SSessionBase SendDeferred(BoosterRemoveResponse value) =>
            SendDeferred(SCCategory.Booster, SCBooster.Remove, (SPacketWriter writer) =>
            {
                writer.Write(value.Id);
            });

        public SSessionBase SendDeferred(BoosterAddResponse value) =>
            SendDeferred(SCCategory.Booster, SCBooster.Add, (SPacketWriter writer) =>
            {
                writer.Write(value.Id);
                writer.Write(value.PrototypeId);
                writer.Write(value.Duration);
            });

        public SSessionBase SendDeferred(CharacterGestureUpdateSlotsResponse value) =>
            SendDeferred(SCCategory.Gesture, SCGesture.SlotUpdate, (SPacketWriter writer) =>
            {
                foreach (uint id in value.Values)
                    writer.Write(id);
            });

        public SSessionBase SendDeferred(GateEnterResponse value) =>
            SendDeferred(SCCategory.Login, SCLogin.EnterServerRes, (SPacketWriter writer) =>
            {
                writer.WriteGateEnterResult(value.Result);
                writer.Write(value.AccountId);
            });

        public SSessionBase SendDeferred(GateCharacterMarkAsFavoriteResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.RepresentativeChange, (SPacketWriter writer) =>
            {
                writer.Write(value.AccountId);
                writer.Write(value.CharacterId);
                writer.WriteHero(value.Hero);
                writer.Write(value.Level);
                writer.WriteByteLengthUnicodeString(value.CharacterName);
                writer.Write(value.PhotoId);
                writer.Write(value.Date);
                writer.Write(value.Error);
            });

        public SSessionBase SendDeferred(GateCharacterListResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.ListRes, (SPacketWriter writer) =>
            {
                writer.Write((byte)value.Characters.Count);
                foreach (CharacterShared character in value.Characters)
                    writer.WriteCharacter(character);

                writer.Write(value.LastSelected);
                writer.Write(byte.MinValue);
                writer.Write((byte)1);
                writer.Write(value.InitializeTime);
                writer.Write(uint.MinValue);
                writer.Write((ulong)1262271600); // dec/31/2009 | DOS DATE | IDK what a date
                writer.Write((byte)17);
                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
            });

        public SSessionBase SendDeferred(GateCharacterChangeBackgroundResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.ChangeBackground, (SPacketWriter writer) =>
            {
                writer.Write(value.AccountId);
                writer.Write(value.BackgroundId);
                writer.Write(uint.MinValue);
            });

        public SSessionBase SendAsync(SGateCharacterSelectResponse value) =>
            SendDeferred(SCCategory.Character, SCCharacter.SelectRes, (SPacketWriter writer) =>
            {
                writer.Write(value.CharacterId);
                writer.Write(value.AccountId);

                writer.Write(value.ServerId);
                writer.Write(value.JumpId);
                writer.Write(value.PortalId);
                writer.Write(value.Map.Seq);
                writer.Write(value.ParentMap.Seq);

                writer.WriteNumberLengthUtf8String(value.EndPoint.Ip);
                writer.Write(value.EndPoint.Port);
                writer.Write(value.Pos);
                writer.Write(value.Type);
            });

        public SSessionBase SendDeferred(AuthGateConnectionEndPointResponse endPoint) =>
            SendDeferred(SCCategory.Login, SCLogin.EnterServer, (SPacketWriter writer) =>
            {
                writer.WriteNumberLengthUtf8String(endPoint.Ip);
                writer.Write(endPoint.Port);
            });

        public SSessionBase SendDeferred(SLUserCharacterForServerResponse gates) =>
            SendDeferred(SCCategory.Login, SCLogin.ServerList, (SPacketWriter writer) =>
            {
                writer.Write(gates.LastSelectedId);
                writer.Write((byte)gates.Values.Count());

                foreach (SLUCFSREntity gate in gates.Values)
                {
                    writer.Write(gate.Id);
                    writer.Write(gate.EndPoint.Port);
                    writer.WriteNumberLengthUtf8String(gate.Name);
                    writer.WriteNumberLengthUtf8String(gate.EndPoint.Ip);
                    writer.WriteGateStatus(gate.Status);
                    writer.Write(gate.PlayersOnlineCount);
                    writer.Write(gate.CharactersCount);
                }
            });

        public SSessionBase SendDeferred(Options value) =>
            SendDeferred(SCCategory.Login, SCLogin.OptionLoad, (SPacketWriter writer) =>
            {
                writer.Write(new byte[64]);

                foreach (OptionStatus option in value)
                    writer.WriteOptionStatus(option);
            });

        public SSessionBase SendDeferred(SAuthLoginResponse value) =>
            SendDeferred(SCCategory.Login, SCLogin.Result, (SPacketWriter writer) =>
            {
                writer.Write(value.AccountId);
                writer.Write(value.IsClearTutorial);
                writer.Write(Encoding.ASCII.GetBytes(value.Mac));

                writer.WriteByteLengthUnicodeString(value.ErrorMessage);
                writer.WriteAuthLoginErrorMessageCode(value.ErrorCode);

                writer.Write(byte.MinValue);
                writer.Write(value.LoginType);
                writer.WriteByteLengthUnicodeString(value.AuthId);
                writer.Write(value.SessionKey);

                writer.Write(value.GameMasterPower);
                writer.Write(value.BrithYear);
                writer.Write(value.BrithMonth);
                writer.Write(value.BrithDay);
            });

        public SSessionBase SendDeferred(SWorldCurrentDataResponse value) =>
            SendDeferred(SCCategory.World, SCWorld.CurDate, (SPacketWriter writer) =>
            {
                writer.Write(value.UnixTimeSeconds);
                writer.Write(value.Year);
                writer.Write(value.Month);
                writer.Write(value.Day);
                writer.Write(value.Hour);
                writer.Write(value.Minute);
                writer.Write(value.Second);
                writer.Write(value.IsDaylightSavingTime);
            });
    }

    public abstract partial class SSessionBase : TcpSession
    {
        public HandlerPermission Permission { get; set; } = HandlerPermission.Anonymous;

        public SSessionBase SendDeferred(SCCategory category, object command, Action<SPacketWriter> func)
        {
            using SPacketWriter writer = new(category, command);
            func(writer);

            return SendDeferred(writer);
        }

        public SSessionBase SendDeferred(SCCategory category, object command)
        {
            using SPacketWriter writer = new(category, command);
            return SendDeferred(writer);
        }

        private SSessionBase SendDeferred(SPacketWriter writer)
        {
            if (!SendAsync(PacketUtils.Pack(writer), 0, writer.BaseStream.Length))
                NetworkUtils.DropNetwork();

            return this;
        }

        protected SSessionBase(SServerBase server, HandlerProvider provider, ILogger logger) : base(server) =>
            (_provider, _logger) = (provider, logger);

        protected override void OnDisconnected() =>
            _logger.LogDebug($"{Id} disconnected");

        protected override void OnConnected() =>
            _logger.LogDebug($"{Id} connected");

        protected override void OnError(SocketError error) =>
            _logger.LogError($"{error}");

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            using MemoryStream ms = new(buffer, (int)offset, (int)size, false);
            using BinaryReader br = new(ms);

            try
            {
                do
                {
                    // SoulWorker Magic
                    ms.Position += sizeof(ushort);

                    // Packet Length
                    int length = br.ReadUInt16() - Defines.PacketUnEncryptedHeaderSize;

                    // ???
                    ms.Position += sizeof(byte);

                    ProcessPacket(br.ReadBytes(length));
                } while (br.BaseStream.Position < br.BaseStream.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shit happened");
#if !DEBUG
                Disconnect();
#endif
                return;
            }
        }

        private void ProcessPacket(byte[] raw)
        {
            PacketUtils.Exchange(ref raw);

            using MemoryStream ms = new(raw, false);
            using BinaryReader br = new(ms);

            ushort opcode = br.ReadUInt16();
            DebugLogOpcode(opcode);

            SHPEntity handler = _provider[opcode];

            if (handler.Permission == Permission)
                handler.Method.Invoke(this, br);
        }

        [Conditional("DEBUG")]
        private void DebugLogOpcode(ushort opcode) =>
            _logger.LogDebug($"@event [0x{opcode:X4}] {(ServerOpcode)opcode}");

        private readonly HandlerProvider _provider;
        private readonly ILogger _logger;
    }
}

// https://youtu.be/UnIhRpIT7nc
// https://youtu.be/iceS6BvhuQ8