using ow.Framework.FS;
using ow.Framework.FS.Enums;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Service.Login.Game;
using System.Collections.Generic;
using System.Text;

namespace ow.Service.Login.Network.Extensions
{
    internal static class GameSessionExtension
    {
        internal static GameSession SendGateConnect(this GameSession session, GateInstance gate)
        {
            using PacketWriter writer = new(ClientOpcode.GateConnect);

            writer.WriteNumberLengthUtf8String(gate.Ip);
            writer.Write(gate.Port);

            return session.SendAsync(writer);
        }

        internal static GameSession SendGateList(this GameSession session, IReadOnlyList<PersonalGate> infos)
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

            return session.SendAsync(writer);
        }

        internal static GameSession SendGameOptions(this GameSession session, OptionsStatuses options)
        {
            using PacketWriter writer = new(ClientOpcode.OptionLoad);

            writer.Write(new byte[64]);

            foreach (OptionStatus option in options)
                writer.WriteOptionStatus(option);

            return session.SendAsync(writer);
        }

        internal static GameSession SendLogin(this GameSession session, int accountId, string mac, ulong sessionKey)
        {
            using PacketWriter writer = new(ClientOpcode.LoginResult);

            writer.Write(accountId);

            writer.WriteLoginResponseType(ResponseType.Success);
            writer.Write(Encoding.ASCII.GetBytes(mac));

            writer.Write(byte.MinValue);
            writer.WriteByteLengthUnicodeString(string.Empty);
            writer.Write(TableMessage.None);

            writer.Write((byte)1);
            writer.WriteByteLengthUnicodeString(_unknownString);

            writer.Write(sessionKey);
            writer.Write(byte.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(byte.MinValue);

            return session.SendAsync(writer);
        }

        internal static GameSession SendLogin(this GameSession session, TableMessage code, string message = "")
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

            return session.SendAsync(writer);
        }

        private static readonly uint _emptyAccountId = uint.MaxValue;
        private static readonly ulong _emptySessionKey = ulong.MinValue;
        private static readonly string _emptyMac = "00-00-00-00-00-00";
        private static readonly string _unknownString = "134006893";
    }
}