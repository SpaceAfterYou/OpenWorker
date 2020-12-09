using ow.Framework.Game.Types;
using ow.Framework.IO.Network;
using ow.Service.Gate.Game.Types;
using System;
using System.Linq;

namespace ow.Service.Gate.Game.Extensions
{
    public static class PacketWriterExtension
    {
        public static void Write(this PacketWriter writer, Character value)
        {
            writer.WriteCharacterMainData(value);
            writer.WriteCharacterWeaponData(value);
            writer.WriteCharacterFashionData(value);
            writer.WriteCharacterMetaData(value);
        }

        public static void Write(this PacketWriter writer, GateEnterResultType value) =>
            writer.Write((byte)value);

        private static void WriteCharacterMainData(this PacketWriter writer, Character value)
        {
            writer.Write(value.Id);
            writer.WriteByteLengthUnicodeString(value.Name);
            writer.WriteHeroId(value.Hero);
            writer.Write(value.Advancement);
            writer.Write(value.PortraitId);
            writer.Write(value.Appearance);
            writer.Write(value.Level);
            writer.Write(new byte[10]);
        }

        private static void WriteCharacterWeaponData(this PacketWriter writer, Character value)
        {
            if (value.Storage.EquippedGearStorage[(int)EquippedGearSlotType.Weapon] is Item weapon)
            {
                writer.Write(weapon.UpgradeLevel);
                writer.Write(weapon.PrototypeId);
            }
            else
            {
                writer.Write(byte.MinValue);
                writer.Write(uint.MaxValue);
            }

            writer.Write(byte.MinValue);
            writer.Write(uint.MaxValue);
        }

        private static void WriteCharacterFashionData(this PacketWriter writer, Character value)
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

        private static void WriteCharacterMetaData(this PacketWriter writer, Character value)
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
            writer.Write(uint.MinValue); // 6 Unknown1
            writer.Write(uint.MinValue); // 7 Unknown2
            writer.Write(uint.MinValue); // 8 Stamina???
            writer.Write(maxStamina); // 9 Max Stamina
            writer.Write(uint.MinValue); // 10 Unknown4
            writer.Write(uint.MinValue); // 11 Unknown5
            writer.Write(moveSpeed);
            writer.Write(attackSpeed);
            writer.Write(ushort.MinValue); // Unknown5
            writer.Write(byte.MinValue); // Unknown6
            writer.Write(primaryEnergy);
            writer.Write(extraEnergy);
            writer.Write(uint.MinValue);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);
            writer.Write(uint.MinValue);
            writer.Write(ushort.MinValue);
            writer.Write(uint.MinValue); // 1
            writer.Write(uint.MinValue); // 2
            writer.Write(value.SlotId);
            writer.Write(uint.MinValue);
            writer.Write(byte.MinValue);
            writer.Write(uint.MinValue);
        }

        private static void Write(this PacketWriter writer, Appearance value)
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

        private static void Write(this PacketWriter writer, Hair hair)
        {
            writer.Write(hair.Style);
            writer.Write(hair.Color);
        }

        public static void Write(this PacketWriter writer, Place value)
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