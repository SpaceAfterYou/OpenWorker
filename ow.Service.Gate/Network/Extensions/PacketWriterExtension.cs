using ow.Framework.Game;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Types;
using System;
using System.Linq;

namespace ow.Service.Gate.Network.Extensions
{
    internal static class PacketWriterExtension
    {
        internal static void Write(this PacketWriter writer, Character value)
        {
            writer.WriteCharacterMainData(value);
            writer.WriteCharacterWeaponData(value);
            writer.WriteCharacterFashionData(value);
            writer.WriteCharacterMetaData(value);
        }

        internal static void Write(this PacketWriter writer, GateEnterResultType value) =>
            writer.Write((byte)value);

        private static void WriteCharacterMainData(this PacketWriter writer, Character value)
        {
            writer.Write(value.Id);
            writer.WriteByteLengthUnicodeString(value.Name);
            writer.WriteHeroId(value.Hero);
            writer.Write(value.Advancement);
            writer.Write(value.Photo.Id);
            writer.Write(value.Appearance);
            writer.Write(value.Level);
            writer.Write(new byte[10]);
        }

        private static void WriteCharacterWeaponData(this PacketWriter writer, Character value)
        {
            if (value.Storage.EquippedGearStorage[(int)EquippedGearSlot.Weapon] is Item weapon)
            {
                writer.Write(weapon.UpgradeLevel);
                writer.Write(weapon.PrototypeId);
            }
            else
            {
                writer.Write(byte.MinValue);
                writer.Write(uint.MinValue);
            }

            writer.Write(byte.MinValue);
            writer.Write(-1);
        }

        private static void WriteCharacterFashionData(this PacketWriter writer, ICharacter value)
        {
            void WriteFashionEntry(int prototypeId = -1, uint color = uint.MinValue)
            {
                writer.Write(-1);
                writer.Write(-1);
                writer.Write(prototypeId);
                writer.Write(color);

                writer.Write(-1);
                writer.Write(-1);
                writer.Write(-1);
                writer.Write(uint.MinValue);
            }

            foreach (var (view, battle) in value.Storage.EquippedViewFashionStorage.Zip(value.Storage.EquippedBattleFashionStorage, Tuple.Create))
            {
                if (view is not null)
                {
                    WriteFashionEntry(view.PrototypeId, view.Color);
                    continue;
                }

                if (battle is not null)
                {
                    WriteFashionEntry(battle.PrototypeId, battle.Color);
                    continue;
                }

                WriteFashionEntry();
            }
        }

        private static void WriteCharacterMetaData(this PacketWriter writer, ICharacter value)
        {
            const uint currentHp = 0;
            const uint maxHp = 0;
            const uint currentSg = 0;
            const uint maxSg = 0;
            const uint maxStamina = 0;
            const float moveSpeed = 1.0f;
            const float attackSpeed = 1.0f;
            const ushort primaryEnergy = 0;
            const ushort extraEnergy = 0;
            const uint titlePrimary = 0;
            const uint titleSecondary = 0;
            const uint guildId = 0;
            const string guildName = "";

            writer.Write(uint.MinValue); // Unknown3
            writer.Write(titlePrimary);
            writer.Write(titleSecondary);
            writer.Write(guildId);
            writer.WriteByteLengthUnicodeString(guildName);
            writer.Write(uint.MinValue); // 1 Unknown4
            writer.Write(currentHp); // 2
            writer.Write(maxHp); // 3
            writer.Write(currentSg); // 4
            writer.Write(maxSg); // 5
            writer.Write(uint.MinValue); // 6
            writer.Write(uint.MinValue); // 7
            writer.Write(uint.MinValue); // 8 Stamina???
            writer.Write(maxStamina); // 9 Max Stamina
            writer.Write(uint.MinValue); // 10
            writer.Write(uint.MinValue); // 11
            writer.Write(moveSpeed);
            writer.Write(attackSpeed);
            writer.Write(uint.MinValue); // 00 00 00 00
            // writer.Write(uint.MinValue); // 00 00 00 00
            writer.Write(primaryEnergy);
            writer.Write(extraEnergy);
            writer.Write(byte.MinValue); // 00
            writer.Write(byte.MinValue); // 08
            writer.Write(uint.MinValue); // 95 36 68 3B
            writer.Write(ushort.MinValue); // 00 00
            writer.Write(uint.MinValue); // 00 00 00 00
            writer.Write(uint.MinValue); // 00 00 00 00
            writer.Write(value.Slot); // 01
            writer.Write(uint.MinValue); // 00 00 00 00
            writer.Write(byte.MinValue); // 00
            writer.Write(uint.MinValue); // 00 00 00 00
        }

        private static void Write(this PacketWriter writer, ICharacterAppearance value)
        {
            writer.Write(ushort.MinValue); // 1
            writer.Write(ushort.MinValue); // 1
            writer.Write(value.Hair); // 2
            writer.Write(value.EyeColor); // 3
            writer.Write(value.SkinColor); // 3
            writer.Write(value.EquippedHair); // 4
            writer.Write(value.EquippedEyeColor); // 5
            writer.Write(value.EquippedSkinColor); // 5
        }

        private static void Write(this PacketWriter writer, ICharacterHair hair)
        {
            writer.Write(hair.Style);
            writer.Write(hair.Color);
        }

        internal static void Write(this PacketWriter writer, Place value)
        {
            writer.Write(value.Location);
            writer.Write((ulong)0);
            writer.WriteVector3(value.Position);
            writer.Write(value.Rotation);
            writer.Write((float)0); // ??? armour gage ???
            writer.Write((float)0); // ??? armour gage ???
        }
    }
}