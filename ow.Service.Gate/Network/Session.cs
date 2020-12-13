using Microsoft.Extensions.Logging;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Providers;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Types;
using ow.Service.Gate.Network.Extensions;
using System;
using System.Linq;

namespace ow.Service.Gate.Network
{
    public sealed class Session : GameSession
    {
        internal Account Account { get; set; }
        internal Characters Characters { get; set; }
        internal ICharacterBackgroundTableEntity Background { get; set; }

        public Session(Server server, HandlerProvider provider, ILogger<Session> logger) :
            base(server, provider, logger)
        {
        }

        internal Session SendGateEnterResult()
        {
            using PacketWriter writer = new(ClientOpcode.GateEnter);

            writer.Write(GateEnterResultType.Success);
            writer.Write(Account.Id);

            return (Session)SendAsync(writer);
        }

        internal Session SendFavoriteCharacter()
        {
            using PacketWriter writer = new(ClientOpcode.CharacterMakrAsFavorite);

            writer.Write(Account.Id);
            writer.Write(Characters.Favorite.Id);
            writer.Write(ushort.MinValue);
            writer.WriteByteLengthUnicodeString(Characters.Favorite.Name);
            writer.Write(Characters.Favorite.Photo.Id);
            writer.Write(uint.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(uint.MinValue);

            return (Session)SendAsync(writer);
        }

        internal Session SendCharactersList()
        {
            using PacketWriter writer = new(ClientOpcode.CharactersList);

            Character[] characters = Characters.Where(c => c is not null).ToArray();

            writer.Write((byte)characters.Length);
            foreach (Character character in characters)
                writer.Write(character);

            if (characters.Length > 0 && Characters.LastSelected is null)
                writer.Write(characters.First().Id);
            else
                writer.Write(Characters.LastSelected?.Id ?? -1);

            writer.Write(byte.MinValue);
            writer.Write((byte)1);
            writer.Write((ulong)Characters.InitializeTime.TotalSeconds);
            writer.Write(uint.MinValue);
            writer.Write((ulong)1262271600); // dec/31/2009
            writer.Write((byte)17);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);

            return (Session)SendAsync(writer);
        }

        internal Session SendCharacterBackground()
        {
            using PacketWriter writer = new(ClientOpcode.CharacterChangeBackground);

            writer.Write(Account.Id);
            writer.Write(Background.Id);
            writer.Write(uint.MinValue);

            return (Session)SendAsync(writer);
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

            return (Session)SendAsync(writer);
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

            return (Session)SendAsync(writer);
        }
    }
}