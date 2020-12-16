using DefaultEcs;
using ow.Framework.Game.Character;
using ow.Framework.Game.Datas;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Opcodes;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Enums;
using ow.Service.Gate.Game.Repository;
using System.Linq;

namespace ow.Service.Gate.Network.Extensions
{
    public static class GameSessionExtension
    {
        internal static GameSession SendGateEnterResult(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.GateEnter);

            writer.Write(GateEnterResult.Success);

            Account account = session.Entity.Get<Account>();
            writer.Write(account.Id);

            return session.SendAsync(writer);
        }

        internal static GameSession SendFavoriteCharacter(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterMakrAsFavorite);

            Account account = session.Entity.Get<Account>();
            writer.Write(account.Id);

            Characters characters = session.Entity.Get<Characters>();
            writer.Write(characters.Favorite.Id);
            writer.Write(ushort.MinValue);
            writer.WriteByteLengthUnicodeString(characters.Favorite.Name);
            writer.Write(characters.Favorite.Photo.Id);
            writer.Write(uint.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(uint.MinValue);

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharactersList(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharactersList);

            Characters characters = session.Entity.Get<Characters>();
            Entity[] entities = characters.Where(c => c.Has<EntityCharacter>()).ToArray();

            writer.Write((byte)entities.Length);
            foreach (Entity entity in entities)
                writer.WriteCharacter(entity);

            writer.Write(characters.LastSelected?.Id ?? -1);
            writer.Write(byte.MinValue);
            writer.Write((byte)1);
            writer.Write((ulong)characters.InitializeTime.TotalSeconds);
            writer.Write(uint.MinValue);
            writer.Write((ulong)1262271600); // dec/31/2009
            writer.Write((byte)17);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterBackground(this GameSession session)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterChangeBackground);

            Account account = session.Entity.Get<Account>();
            writer.Write(account.Id);

            CharacterBackgroundTableEntity background = session.Entity.Get<CharacterBackgroundTableEntity>();
            writer.Write(background.Id);

            writer.Write(uint.MinValue);

            return session.SendAsync(writer);
        }

        internal static GameSession SendCharacterSelect(this GameSession session, DistrictRepository districts)
        {
            using PacketWriter writer = new(ClientOpcode.CharacterSelect);

            Characters characters = session.Entity.Get<Characters>();
            writer.Write(characters.LastSelected.Id);

            Account account = session.Entity.Get<Account>();
            writer.Write(account.Id);

            Place place = session.Entity.Get<Place>();

            writer.Write(new byte[28]);

            DistrictInstance district = districts[place.District.Id];
            writer.WriteNumberLengthUtf8String(district.Ip);
            writer.Write(district.Port);

            writer.WritePlace(place);

            writer.Write(new byte[12]);

            return session.SendAsync(writer);
        }
    }
}