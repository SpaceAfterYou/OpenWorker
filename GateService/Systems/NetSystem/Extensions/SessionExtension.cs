using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using GateService.Systems.GameSystem;
using GateService.Systems.GameSystem.Extensions;
using GateService.Systems.GameSystem.Types;
using System;

namespace GateService.Systems.NetSystem.Extensions
{
    internal static class SessionExtension
    {
        internal static Session SendGateEnterResult(this Session session)
        {
            using WriterPacket pw = new(ClientOpcode.GateEnter);

            pw.Write(GateEnterResultType.Success);
            pw.Write(session.Account.Id);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendCharactersList(this Session session)
        {
            using WriterPacket pw = new(ClientOpcode.CharactersList);

            pw.Write((byte)session.Characters.Count);
            foreach (var character in session.Characters) { pw.Write(character); }

            pw.Write(session.Characters.LastSelectedId);
            pw.Write((ushort)0);
            pw.Write((ulong)session.Characters.InitializeTime.TotalSeconds);
            pw.Write((uint)0);
            pw.Write((ulong)1262271600); // dec/31/2009
            pw.Write((byte)17);
            pw.Write((byte)0);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendCharacterSelect(this Session session, Character character, District district)
        {
            using WriterPacket pw = new(ClientOpcode.CharacterSelect);

            pw.Write(character.Id);
            pw.Write(session.Account.Id);
            pw.Write(new byte[28]);
            pw.WriteNumberLengthUtf8String(district.Address.ToString());
            pw.Write((ushort)district.Port);
            pw.Write(character.Place);
            pw.Write(new byte[12]);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendCurrentDate(this Session session)
        {
            using WriterPacket pw = new(ClientOpcode.CurrentDate);

            DateTimeOffset dateTime = DateTimeOffset.Now;

            pw.Write(dateTime.ToUnixTimeSeconds());
            pw.Write((ushort)dateTime.Year);
            pw.Write((ushort)dateTime.Month);
            pw.Write((ushort)dateTime.Day);
            pw.Write((ushort)dateTime.Hour);
            pw.Write((ushort)dateTime.Minute);
            pw.Write((ushort)dateTime.Second);
            pw.Write(Convert.ToUInt16(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime)));

            return session.SendAsync(pw) as Session;
        }
    }
}