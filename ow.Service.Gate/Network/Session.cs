using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Extensions;
using ow.Service.Gate.Game.Types;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using System;

namespace ow.Service.Gate.Network
{
    public sealed class Session : SwSession
    {
        public Account Account { get; set; }
        public Characters Characters { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) :
            base(server, provider, logger)
        {
        }

        internal Session SendGateEnterResult()
        {
            using PacketWriter writer = new(ClientOpcode.GateEnter);

            writer.Write(GateEnterResultType.Success);
            writer.Write(Account.Id);

            return SendAsync(writer) as Session;
        }

        internal Session SendCharactersList()
        {
            using PacketWriter writer = new(ClientOpcode.CharactersList);

            writer.Write((byte)Characters.Count);
            foreach (var character in Characters) { writer.Write(character); }

            writer.Write(Characters.LastSelected?.Id ?? uint.MaxValue);
            writer.Write(ushort.MinValue);
            writer.Write((ulong)Characters.InitializeTime.TotalSeconds);
            writer.Write(uint.MinValue);
            writer.Write((ulong)1262271600); // dec/31/2009
            writer.Write((byte)17);
            writer.Write(byte.MinValue);

            return SendAsync(writer) as Session;
        }

        internal Session SendCharacterSelect(Character character, District district)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterSelect);

            writer.Write(character.Id);
            writer.Write(Account.Id);
            writer.Write(new byte[28]);
            writer.WriteNumberLengthUtf8String(district.Ip);
            writer.Write(district.Port);
            writer.Write(character.Place);
            writer.Write(new byte[12]);

            return SendAsync(writer) as Session;
        }

        internal Session SendCurrentDate()
        {
            using PacketWriter writer = new(ClientOpcode.CurrentDate);

            DateTimeOffset dateTime = DateTimeOffset.Now;

            writer.Write(dateTime.ToUnixTimeSeconds());
            writer.Write((ushort)dateTime.Year);
            writer.Write((ushort)dateTime.Month);
            writer.Write((ushort)dateTime.Day);
            writer.Write((ushort)dateTime.Hour);
            writer.Write((ushort)dateTime.Minute);
            writer.Write((ushort)dateTime.Second);
            writer.Write(Convert.ToUInt16(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime)));

            return SendAsync(writer) as Session;
        }
    }
}
