using Core.DatabaseSystem.Accounts;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using LoginService.Systems.GameSystem;
using LoginService.Systems.GameSystem.Extensions;
using SoulWorker.Types;
using System.Collections.Generic;

namespace LoginService.Systems.NetSystem.Extensions
{
    public static class SessionExtension
    {
        internal static Session SendGateConnect(this Session session, Gate gate)
        {
            using WriterPacket pw = new(ClientOpcode.GateConnect);

            pw.WriteNumberLengthUtf8String(gate.EndPoint.Address.ToString());
            pw.Write((ushort)gate.EndPoint.Port);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendGateList(this Session session, IReadOnlyList<PersonalGate> infos)
        {
            using WriterPacket pw = new(ClientOpcode.GateList);

            pw.Write((byte)0);
            pw.Write((byte)infos.Count);

            foreach (var info in infos)
            {
                pw.Write(info.Gate.Id);
                pw.Write((ushort)info.Gate.EndPoint.Port);
                pw.WriteNumberLengthUtf8String(info.Gate.Name);
                pw.WriteNumberLengthUtf8String(info.Gate.EndPoint.ToString());
                pw.Write(info.Gate.Status);
                pw.Write((byte)0);
                pw.Write((byte)0);
                pw.Write((byte)0);
                pw.Write(info.Gate.PlayersOnlineCount);
                pw.Write((ushort)0);
                pw.Write(info.CharactersCount);
            }

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendOptionLoad(this Session session, Options options)
        {
            using WriterPacket pw = new(ClientOpcode.OptionLoad);

            pw.Write(new byte[64]);
            pw.Write(options.LoginBonus);
            pw.Write(options.SecondaryPassword);
            pw.Write(options.PremiumShop);
            pw.Write(options.Option4);
            pw.Write(options.Option5);
            pw.Write(options.Option6);
            pw.Write(options.Option7);
            pw.Write(options.Option8);
            pw.Write(options.Option9);
            pw.Write(options.Option10);
            pw.Write(options.Option11);
            pw.Write(options.Option12);
            pw.Write(options.Option13);
            pw.Write(options.Option14);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendLogin(this Session session, AccountModel model)
        {
            string unknownString = "134006893";

            using WriterPacket pw = new(ClientOpcode.LoginResult);

            pw.Write(model.Id);

            pw.Write(ResponseType.Success);
            //pw.Write(Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(model.Mac)));
            pw.WriteNumberLengthUtf8String(model.Mac);

            pw.Write((byte)0);
            pw.WriteByteLengthUnicodeString(string.Empty);
            pw.Write(TableMessageId.None);

            pw.Write((byte)1);
            pw.WriteByteLengthUnicodeString(unknownString);

            pw.Write(model.SessionKey);
            pw.Write((byte)0);
            pw.Write((uint)0);
            pw.Write((byte)0);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendLogin(this Session session, TableMessageId code, string message = "")
        {
            using WriterPacket pw = new(ClientOpcode.LoginResult);

            pw.Write(_emptyAccountId);

            pw.Write(ResponseType.Failure);
            //pw.Write(_emptyMac);
            pw.WriteNumberLengthUtf8String(_emptyMac);

            pw.Write((byte)0);
            pw.WriteByteLengthUnicodeString(message);
            pw.Write(code);

            pw.Write((byte)1);
            pw.WriteByteLengthUnicodeString(string.Empty);

            pw.Write(_emptySessionKey);
            pw.Write((byte)0);
            pw.Write((uint)0);
            pw.Write((byte)0);

            return session.SendAsync(pw) as Session;
        }

        private static readonly int _emptyAccountId = -1;
        private static readonly ulong _emptySessionKey = 0;
        private static readonly string _emptyMac = "00-00-00-00-00-00";
        //private static readonly byte[] _emptyMac = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes("00-00-00-00-00-00"));
    }
}