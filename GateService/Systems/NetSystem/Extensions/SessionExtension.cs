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
            using WriterPacket writer = new(ClientOpcode.GateEnter);

            writer.Write(GateEnterResultType.Success);
            writer.Write(session.Account.Id);

            return session.SendAsync(writer) as Session;
        }

        internal static Session SendCharactersList(this Session session)
        {
            using WriterPacket writer = new(ClientOpcode.CharactersList);

            writer.Write((byte)session.Characters.Count);
            foreach (var character in session.Characters) { writer.Write(character); }

            writer.Write(session.Characters.LastSelectedId);
            writer.Write((ushort)0);
            writer.Write((ulong)session.Characters.InitializeTime.TotalSeconds);
            writer.Write((uint)0);
            writer.Write((ulong)1262271600); // dec/31/2009
            writer.Write((byte)17);
            writer.Write((byte)0);

            return session.SendAsync(writer) as Session;
        }

        internal static Session SendCharacterSelect(this Session session, Character character, District district)
        {
            using WriterPacket writer = new(ClientOpcode.CharacterSelect);

            writer.Write(character.Id);
            writer.Write(session.Account.Id);
            writer.Write(new byte[28]);
            writer.WriteNumberLengthUtf8String(district.Address.ToString());
            writer.Write((ushort)district.Port);
            writer.Write(character.Place);
            writer.Write(new byte[12]);

            return session.SendAsync(writer) as Session;
        }

        internal static Session SendCurrentDate(this Session session)
        {
            using WriterPacket writer = new(ClientOpcode.CurrentDate);

            DateTimeOffset dateTime = DateTimeOffset.Now;

            writer.Write(dateTime.ToUnixTimeSeconds());
            writer.Write((ushort)dateTime.Year);
            writer.Write((ushort)dateTime.Month);
            writer.Write((ushort)dateTime.Day);
            writer.Write((ushort)dateTime.Hour);
            writer.Write((ushort)dateTime.Minute);
            writer.Write((ushort)dateTime.Second);
            writer.Write(Convert.ToUInt16(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime)));

            return session.SendAsync(writer) as Session;
        }
    }
}