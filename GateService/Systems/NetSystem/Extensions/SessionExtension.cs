using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Packets;
using GateService.Systems.GameSystem;
using System;

namespace GateService.Systems.NetSystem.Extensions
{
    internal static class SessionExtension
    {
        internal static Session SendGateEnterResult(this Session session)
        {
            using WriterPacket pw = new(ClientOpcode.GateEnter);

            pw.Write((byte)0); // result
            pw.Write(session.Account.Id);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendCharactersList(this Session session)
        {
            using WriterPacket pw = new(ClientOpcode.CharactersList);

            pw.Write((byte)characters.Count);
            foreach (var character in characters.Values)
            {
                var equipped = character.GetComponent<EquippedStorage>();

                pw.WriteMainData(character);
                pw.WriteWeaponData(equipped);
                pw.WriteFashionData(equipped);
                pw.WriteGateCharacterMetaData(character);
            }

            pw.Write(characters.Last?.Id ?? -1);
            pw.Write((ushort)0);
            pw.Write((ulong)characters.InitializeTime.TotalSeconds);
            pw.Write((uint)0);
            pw.Write((ulong)1262271600); // dec/31/2009
            pw.Write((byte)17);
            pw.Write((byte)0);

            return session.SendAsync(pw) as Session;
        }

        internal static Session SendCharacterSelect(this Session session, Character character, DistrictConnectionInfo connectionInfo)
        {
            using WriterPacket pw = new(ClientOpcode.CharacterSelect);

            pw.Write(character.Id);
            pw.Write(session.Account.Id);
            pw.Write(new byte[28]);
            pw.WriteUtf8String(ip);
            pw.Write(connectionInfo.Port);
            pw.Write(character.WorldPosition);
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