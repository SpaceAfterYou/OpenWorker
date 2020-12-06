using Core.Systems.DatabaseSystem.Accounts;
using Core.Systems.NetSystem;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using Core.Systems.NetSystem.Providers;
using LoginService.Systems.GameSystem;
using LoginService.Systems.GameSystem.Extensions;
using Microsoft.Extensions.Logging;
using SoulWorker.Types;
using System.Collections.Generic;

namespace LoginService.Systems.NetSystem
{
    public sealed class Session : SwSession
    {
        public Account Account { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) :
            base(server, provider, logger)
        {
        }

        internal Session SendGateConnect(Gate gate)
        {
            using WriterPacket writer = new(ClientOpcode.GateConnect);

            writer.WriteNumberLengthUtf8String(gate.EndPoint.Address.ToString());
            writer.Write((ushort)gate.EndPoint.Port);

            return SendAsync(writer) as Session;
        }

        internal Session SendGateList(IReadOnlyList<PersonalGate> infos)
        {
            using WriterPacket writer = new(ClientOpcode.GateList);

            writer.Write(byte.MinValue);
            writer.Write((byte)infos.Count);

            foreach (PersonalGate info in infos)
            {
                writer.Write(info.Gate.Id);
                writer.Write((ushort)info.Gate.EndPoint.Port);
                writer.WriteNumberLengthUtf8String(info.Gate.Name);
                writer.WriteNumberLengthUtf8String(info.Gate.EndPoint.ToString());
                writer.Write(info.Gate.Status);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write(info.Gate.PlayersOnlineCount);
                writer.Write((ushort)0);
                writer.Write(info.CharactersCount);
            }

            return SendAsync(writer) as Session;
        }

        internal Session SendOptionLoad(Options options)
        {
            using WriterPacket writer = new(ClientOpcode.OptionLoad);

            writer.Write(new byte[64]);
            writer.Write(options.LoginBonus);
            writer.Write(options.SecondaryPassword);
            writer.Write(options.PremiumShop);
            writer.Write(options.Option4);
            writer.Write(options.Option5);
            writer.Write(options.Option6);
            writer.Write(options.Option7);
            writer.Write(options.Option8);
            writer.Write(options.Option9);
            writer.Write(options.Option10);
            writer.Write(options.Option11);
            writer.Write(options.Option12);
            writer.Write(options.Option13);
            writer.Write(options.Option14);

            return SendAsync(writer) as Session;
        }

        internal Session SendLogin(AccountModel model)
        {
            using WriterPacket writer = new(ClientOpcode.LoginResult);

            writer.Write(model.Id);

            writer.Write(ResponseType.Success);
            writer.WriteNumberLengthUtf8String(model.Mac);

            writer.Write(byte.MinValue);
            writer.WriteByteLengthUnicodeString(string.Empty);
            writer.Write(TableMessageId.None);

            writer.Write((byte)1);
            writer.WriteByteLengthUnicodeString(_unknownString);

            writer.Write(model.SessionKey);
            writer.Write(byte.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(byte.MinValue);

            return SendAsync(writer) as Session;
        }

        internal Session SendLogin(TableMessageId code, string message = "")
        {
            using WriterPacket writer = new(ClientOpcode.LoginResult);

            writer.Write(_emptyAccountId);

            writer.Write(ResponseType.Failure);
            writer.WriteNumberLengthUtf8String(_emptyMac);

            writer.Write(byte.MinValue);
            writer.WriteByteLengthUnicodeString(message);
            writer.Write(code);

            writer.Write((byte)1);
            writer.WriteByteLengthUnicodeString(_unknownString);

            writer.Write(_emptySessionKey);
            writer.Write(byte.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(byte.MinValue);

            return SendAsync(writer) as Session;
        }

        private static readonly uint _emptyAccountId = uint.MaxValue;
        private static readonly ulong _emptySessionKey = ulong.MinValue;
        private static readonly string _emptyMac = "00-00-00-00-00-00";
        private static readonly string _unknownString = "134006893";
    }
}