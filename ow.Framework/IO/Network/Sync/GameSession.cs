using Microsoft.Extensions.Logging;
using NetCoreServer;
using ow.Framework.Game;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Providers;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using ow.Framework.IO.Network.Sync.Responses.Shared;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ow.Framework.IO.Network.Sync
{
    public abstract partial class SyncSession : TcpSession
    {
        #region Send Characters

        public SyncSession SendAsync(PartyDeleteResponse value) =>
            SendAsync(ClientOpcode.PartyDelete, (PacketWriter writer) =>
            {
                writer.Write(value.Id);
            });

        #endregion Send Characters

        #region Send Characters

        public SyncSession SendCharacterDbLoadSync() =>
            SendAsync(ClientOpcode.CharacterDbLoadSync, (PacketWriter writer) =>
            {
            });

        public SyncSession SendAsync(CharacterToggleWeaponRequest request) =>
            SendAsync(ClientOpcode.CharacterToggleWeapon, (PacketWriter writer) =>
            {
                writer.Write(request.CharacterId);
                writer.WriteVector3(request.Position);
                writer.Write(request.Rotation);
                writer.Write(request.Toggle);
                writer.Write(request.Unknown1);
            });

        public SyncSession SendAsync(NpcOthersInfosResponse value) =>
            SendAsync(ClientOpcode.NpcOtherInfos, (PacketWriter writer) =>
            {
                writer.Write((ushort)value.Values.Count);
                foreach (NpcOthersInfosResponse.Entity entity in value.Values)
                {
                    writer.Write(entity.Id);
                    writer.WriteVector3(entity.Position);
                    writer.Write(entity.Rotation);
                    writer.Write(uint.MinValue);
                    writer.Write(entity.Waypoint);
                    writer.Write(uint.MinValue);
                    writer.WriteNpcVisability(NpcVisablity.Visible);
                    writer.Write(entity.NpcId);
                }
            });

        public SyncSession SendAsync(CharacterInfoResponse value) =>
            SendAsync(ClientOpcode.CharacterInfo, (PacketWriter writer) =>
            {
                writer.WriteCharacter(value.Character);
                writer.WritePlace(value.Place);

                writer.Write(ulong.MinValue); // Exp
                writer.Write(ulong.MinValue); // Zenny

                //writer.Write(uint.MinValue); // 1
                //writer.Write(uint.MinValue); // 2
                //writer.Write(uint.MinValue); // 3
                //writer.Write(uint.MinValue); // 4
                writer.Write(uint.MinValue); // 1
                writer.Write(3758046911); // 2
                writer.Write(473918745); // 3
                writer.Write(75719); // 4
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

                var w = ((MemoryStream)writer.BaseStream).ToArray();
                // var q = ((MemoryStream)writer.BaseStream).GetBuffer();
            });

        public SyncSession SendAsync(CharacterStatsUpdateResponse value) =>
            SendAsync(ClientOpcode.CharacterStatsUpdate, (PacketWriter writer) =>
            {
                writer.Write((byte)0);

                writer.Write(value.Character);
                writer.Write((byte)value.Values.Count);

                foreach (CharacterStatsUpdateResponse.Entity stat in value.Values)
                {
                    writer.Write(stat.Value);
                    writer.WriteCharacterStat(stat.Id);
                }
            });

        public SyncSession SendAsync(CharacterProfileResponse value) =>
            SendAsync(ClientOpcode.CharacterProfileInfo, (PacketWriter writer) =>
            {
                writer.WriteProfileStatus(value.Status);
                writer.WriteByteLengthUnicodeString(value.About);
                writer.WriteByteLengthUnicodeString(value.Note);
            });

        public SyncSession SendAsync(GestureLoadResponse value) =>
            SendAsync(ClientOpcode.GestureLoad, (PacketWriter writer) =>
            {
                foreach (uint gesture in value.Values)
                    writer.Write(gesture);
            });

        public SyncSession SendCharacterPostInfo() =>
            SendAsync(ClientOpcode.PostInfo, (PacketWriter writer) =>
            {
            });

        #endregion Send Characters

        #region Send Chat

        public SyncSession SendAsync(ChatResponse value) =>
            SendAsync(ClientOpcode.ChatMessage, (PacketWriter writer) =>
            {
                writer.Write(value.Character);
                writer.WriteChatType(value.Chat);
                writer.WriteByteLengthUnicodeString(value.Message);
            });

        #endregion Send Chat

        #region Send Maze

        public SyncSession SendAsync(DayEventBoosterResponse value) =>
            SendAsync(ClientOpcode.EventDayEventBoosterList, (PacketWriter writer) =>
            {
                writer.Write((ushort)value.Values.Count);
                foreach (DayEventBoosterResponse.Entity booster in value.Values)
                {
                    writer.Write(booster.Maze);
                    writer.Write(booster.Id);
                }
            });

        #endregion Send Maze

        #region Send Service

        public SyncSession SendAsync(ServiceHeartbeatRequest value) =>
            SendAsync(ClientOpcode.Heartbeat, (PacketWriter writer) =>
            {
                writer.Write(value.Tick);
            });

        public SyncSession SendAsync(DistrictLogOutResponse value) =>
            SendAsync(ClientOpcode.LogOut, (PacketWriter writer) =>
            {
                writer.Write(value.Account);
                writer.Write(value.Character);
                writer.WriteNumberLengthUtf8String(value.Ip);
                writer.Write(value.Port);
                writer.WriteDistrictLogOutWay(DistrictLogOutWay.GoToGateService);
                writer.WriteDistrictLogOutStatus(DistrictLogOutStatus.Success);
            });

        #endregion Send Service

        #region Send World

        public SyncSession SendAsync(DistrictEnterResponse value) =>
            SendAsync(ClientOpcode.WorldEnter, (PacketWriter writer) =>
            {
                writer.Write(uint.MinValue);
                writer.WriteDistrictConnectResult(value.Result);
                writer.WritePlace(value.Place);
                writer.Write(byte.MinValue);
            });

        public SyncSession SendAsync(WorldVersionResponse value) =>
            SendAsync(ClientOpcode.WorldVersion, (PacketWriter writer) =>
            {
                writer.Write(value.Id);
                writer.Write(value.Main);
                writer.Write(value.Sub);
                writer.Write(value.Data);
            });

        #endregion Send World

        #region Send Boosters

        public SyncSession SendAsync(BoosterRemoveResponse value) =>
            SendAsync(ClientOpcode.BoosterRemove, (PacketWriter writer) =>
            {
                writer.Write(value.Id);
            });

        public SyncSession SendAsync(BoosterAddResponse value) =>
            SendAsync(ClientOpcode.BoosterAdd, (PacketWriter writer) =>
            {
                writer.Write(value.Id);
                writer.Write(value.PrototypeId);
                writer.Write(value.Duration);
            });

        #endregion Send Boosters

        public SyncSession SendAsync(GestureUpdateSlotsResponse value) =>
            SendAsync(ClientOpcode.GestureUpdateSlots, (PacketWriter writer) =>
            {
                foreach (uint id in value.Values)
                    writer.Write(id);
            });

        public SyncSession SendAsync(GateEnterResponse value) =>
            SendAsync(ClientOpcode.GateEnter, (PacketWriter writer) =>
            {
                writer.WriteGateEnterResult(value.Result);
                writer.Write(value.AccountId);
            });

        public SyncSession SendAsync(GateCharacterMarkAsFavoriteResponse value) =>
            SendAsync(ClientOpcode.CharacterMarkAsFavorite, (PacketWriter writer) =>
            {
                writer.Write(value.AccountId);
                writer.Write(value.CharacterId);
                writer.Write(ushort.MinValue);
                writer.WriteByteLengthUnicodeString(value.CharacterName);
                writer.Write(value.PhotoId);
                writer.Write(uint.MinValue);
                writer.Write(uint.MinValue);
                writer.Write(uint.MinValue);
            });

        public SyncSession SendAsync(GateCharacterListResponse value) =>
            SendAsync(ClientOpcode.CharactersList, (PacketWriter writer) =>
            {
                writer.Write((byte)value.Characters.Count);
                foreach (CharacterShared character in value.Characters)
                    writer.WriteCharacter(character);

                writer.Write(value.LastSelected);
                writer.Write(byte.MinValue);
                writer.Write((byte)1);
                writer.Write(value.InitializeTime);
                writer.Write(uint.MinValue);
                writer.Write((ulong)1262271600); // dec/31/2009
                writer.Write((byte)17);
                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
            });

        public SyncSession SendAsync(GateCharacterChangeBackgroundResponse value) =>
            SendAsync(ClientOpcode.CharacterChangeBackground, (PacketWriter writer) =>
            {
                writer.Write(value.AccountId);
                writer.Write(value.BackgroundId);
                writer.Write(uint.MinValue);
            });

        public SyncSession SendAsync(GateCharacterSelectResponse value) =>
            SendAsync(ClientOpcode.CharacterSelect, (PacketWriter writer) =>
            {
                writer.Write(value.CharacterId);
                writer.Write(value.AccountId);
                writer.Write(new byte[28]);
                writer.WriteNumberLengthUtf8String(value.EndPoint.Ip);
                writer.Write(value.EndPoint.Port);
                writer.WritePlace(value.Place);
                writer.Write(new byte[12]);
            });

        public SyncSession SendAsync(AuthGateConnectionEndPointResponse endPoint) =>
            SendAsync(ClientOpcode.GateConnect, (PacketWriter writer) =>
            {
                writer.WriteNumberLengthUtf8String(endPoint.Ip);
                writer.Write(endPoint.Port);
            });

        public SyncSession SendAsync(IReadOnlyList<AuthPersonalGateResponse> gates) =>
            SendAsync(ClientOpcode.GateList, (PacketWriter writer) =>
            {
                writer.Write(byte.MinValue);
                writer.Write((byte)gates.Count);

                foreach (AuthPersonalGateResponse gate in gates)
                {
                    writer.Write(gate.Gate.Id);
                    writer.Write(gate.Gate.EndPoint.Port);
                    writer.WriteNumberLengthUtf8String(gate.Gate.Name);
                    writer.WriteNumberLengthUtf8String(gate.Gate.EndPoint.Ip);
                    writer.WriteGateStatus(gate.Gate.Status);
                    writer.Write(byte.MinValue);
                    writer.Write(byte.MinValue);
                    writer.Write(byte.MinValue);
                    writer.Write(gate.Gate.PlayersOnlineCount);
                    writer.Write(ushort.MinValue);
                    writer.Write(gate.CharactersCount);
                }
            });

        public SyncSession SendAsync(Features value) =>
            SendAsync(ClientOpcode.OptionLoad, (PacketWriter writer) =>
            {
                writer.Write(new byte[64]);

                foreach (FeatureStatus option in value)
                    writer.WriteOptionStatus(option);
            });

        public SyncSession SendAsync(AuthLoginResponse value) =>
            SendAsync(ClientOpcode.LoginResult, (PacketWriter writer) =>
            {
                writer.Write(value.AccountId);

                writer.Write(byte.MinValue);
                writer.Write(value.Response == AuthLoginStatus.Failure ? new byte[18] : Encoding.ASCII.GetBytes(value.Mac));

                writer.WriteByteLengthUnicodeString(value.ErrorMessage);
                writer.WriteAuthLoginErrorMessageCode(value.ErrorMessageCode);

                writer.Write(byte.MinValue);
                writer.Write(byte.MinValue);
                writer.WriteByteLengthUnicodeString(value.ErrorMessage);
                writer.Write(value.SessionKey);

                writer.Write(byte.MinValue);
                writer.Write(uint.MinValue);
                writer.Write(byte.MinValue);
            });

        public SyncSession SendAsync(ServiceCurrentDataResponse value) =>
            SendAsync(ClientOpcode.CurrentDate, (PacketWriter writer) =>
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

    public abstract partial class SyncSession : TcpSession
    {
        private readonly HandlerProvider _provider;
        private readonly ILogger _logger;

        public SyncSession SendAsync(ClientOpcode opcode, Action<PacketWriter> func)
        {
            using PacketWriter writer = new(opcode);
            func(writer);

            if (!SendAsync(PacketUtils.Pack(writer), 0, writer.BaseStream.Length))
#if !DEBUG
                throw new NetworkException();
#else
                Debug.Assert(false);
#endif // !DEBUG

            return this;
        }

        protected SyncSession(SyncServer server, HandlerProvider provider, ILogger logger) : base(server) =>
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
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
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

            _provider[opcode].Invoke(this, br);
        }

        [Conditional("DEBUG")]
        private void DebugLogOpcode(ushort opcode)
        {
            ServerOpcode o = (ServerOpcode)ConvertUtils.LeToBeUInt16(opcode);

            if (o != ServerOpcode.Heartbeat)
                _logger.LogDebug($"@event [{(ServerOpcode)ConvertUtils.LeToBeUInt16(opcode)}]");
        }
    }
}