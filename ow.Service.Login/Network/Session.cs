using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using ow.Service.Login.Game;
using ow.Service.Login.Game.Extensions;
using ow.Service.Login.Network.Extensions;
using System.Collections.Generic;
using System.Text;

namespace ow.Service.Login.Network
{
    public sealed class Session : GameSession
    {
        public Account Account { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) :
            base(server, provider, logger)
        {
        }

        internal Session SendGateConnect(GateInstance gate)
        {
            using PacketWriter writer = new(ClientOpcode.GateConnect);

            writer.WriteNumberLengthUtf8String(gate.Ip);
            writer.Write(gate.Port);

            return SendAsync(writer) as Session;
        }

        internal Session SendGateList(IReadOnlyList<PersonalGate> infos)
        {
            using PacketWriter writer = new(ClientOpcode.GateList);

            writer.Write(byte.MinValue);
            writer.Write((byte)infos.Count);

            foreach (PersonalGate info in infos)
            {
                writer.Write(info.Gate.Id);
                writer.Write(info.Gate.Port);
                writer.WriteNumberLengthUtf8String(info.Gate.Name);
                writer.WriteNumberLengthUtf8String(info.Gate.Ip);
                writer.WriteGateStatus(info.Gate.Status);
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
            using PacketWriter writer = new(ClientOpcode.OptionLoad);

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

        internal Session SendLogin(int accountId, string mac, ulong sessionKey)
        {
            using PacketWriter writer = new(ClientOpcode.LoginResult);

            writer.Write(accountId);

            writer.WriteLoginResponseType(ResponseType.Success);
            writer.Write(Encoding.ASCII.GetBytes(mac));

            writer.Write(byte.MinValue);
            writer.WriteByteLengthUnicodeString(string.Empty);
            writer.Write(TableMessageId.None);

            writer.Write((byte)1);
            writer.WriteByteLengthUnicodeString(_unknownString);

            writer.Write(sessionKey);
            writer.Write(byte.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(byte.MinValue);

            return SendAsync(writer) as Session;
        }

        internal Session SendLogin(TableMessageId code, string message = "")
        {
            using PacketWriter writer = new(ClientOpcode.LoginResult);

            writer.Write(_emptyAccountId);

            writer.WriteLoginResponseType(ResponseType.Failure);
            writer.Write(Encoding.ASCII.GetBytes(_emptyMac));

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