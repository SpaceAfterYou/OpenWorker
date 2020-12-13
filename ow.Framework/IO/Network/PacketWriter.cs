using ow.Framework.Game.Character;
using ow.Framework.Game.Enums;
using ow.Framework.Game.Storage.Item;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.Utils;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ow.Framework.IO.Network
{
    public sealed class PacketWriter : BinaryWriter
    {
        public void Write(ICharacter value)
        {
            WriteCharacterMainData(value);
            WriteCharacterWeaponData(value);
            WriteCharacterFashionData(value);
            WriteCharacterMetaData(value);
        }

        private void WriteCharacterMainData(ICharacter value)
        {
            Write(value.Id);
            WriteByteLengthUnicodeString(value.Name);
            WriteHeroId(value.Hero);
            Write(value.Advancement);
            Write(value.Photo.Id);
            Write(value.Appearance);
            Write(value.Level);
            Write(new byte[10]);
        }

        private void WriteCharacterWeaponData(ICharacter value)
        {
            if (value.Storage.EquippedGearStorage.Weapon is IItemStorage weapon)
            {
                Write(weapon.UpgradeLevel);
                Write(weapon.PrototypeId);
            }
            else
            {
                Write(byte.MinValue);
                Write(uint.MinValue);
            }

            Write(byte.MinValue);
            Write(-1);
        }

        private void WriteCharacterFashionData(ICharacter value)
        {
            static void WriteFashionEntry(PacketWriter writer, int prototypeId = -1, uint color = uint.MinValue)
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
                    WriteFashionEntry(this, view.PrototypeId, view.Color);
                    continue;
                }

                if (battle is not null)
                {
                    WriteFashionEntry(this, battle.PrototypeId, battle.Color);
                    continue;
                }

                WriteFashionEntry(this);
            }
        }

        private void WriteCharacterMetaData(ICharacter value)
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

            Write(uint.MinValue); // Unknown3
            Write(titlePrimary);
            Write(titleSecondary);
            Write(guildId);
            WriteByteLengthUnicodeString(guildName);
            Write(uint.MinValue); // 1 Unknown4
            Write(currentHp); // 2
            Write(maxHp); // 3
            Write(currentSg); // 4
            Write(maxSg); // 5
            Write(uint.MinValue); // 6
            Write(uint.MinValue); // 7
            Write(uint.MinValue); // 8 Stamina???
            Write(maxStamina); // 9 Max Stamina
            Write(uint.MinValue); // 10
            Write(uint.MinValue); // 11
            Write(moveSpeed);
            Write(attackSpeed);
            Write(uint.MinValue); // 00 00 00 00
            Write(primaryEnergy);
            Write(extraEnergy);
            Write(byte.MinValue); // 00
            Write(byte.MinValue); // 08
            Write(uint.MinValue); // 95 36 68 3B
            Write(ushort.MinValue); // 00 00
            Write(uint.MinValue); // 00 00 00 00
            Write(uint.MinValue); // 00 00 00 00
            Write(value.Slot); // 01
            Write(uint.MinValue); // 00 00 00 00
            Write(byte.MinValue); // 00
            Write(uint.MinValue); // 00 00 00 00
        }

        private void Write(ICharacterHair hair)
        {
            Write(hair.Style);
            Write(hair.Color);
        }

        private void Write(ICharacterAppearance value)
        {
            Write(ushort.MinValue); // 1
            Write(ushort.MinValue); // 1
            Write(value.Hair); // 2
            Write(value.EyeColor); // 3
            Write(value.SkinColor); // 3
            Write(value.EquippedHair); // 4
            Write(value.EquippedEyeColor); // 5
            Write(value.EquippedSkinColor); // 5
        }

        public void WriteNumberLengthUtf8String(string str)
        {
            Write((ushort)str.Length);
            Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteByteLengthUtf8String(string str)
        {
            Write((ushort)(str.Length * 2));
            Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteNumberLengthChars(char[] str)
        {
            Write((ushort)str.Length);
            Write(str);
        }

        public void WriteNumberLengthUnicodeString(string str)
        {
            Write((ushort)str.Length);
            Write(Encoding.Unicode.GetBytes(str));
        }

        public void WriteByteLengthUnicodeString(string str)
        {
            Write((ushort)(str.Length * 2));
            Write(Encoding.Unicode.GetBytes(str));
        }

        public void WriteVector2(in Vector2 value)
        {
            Write(value.X);
            Write(value.Y);
        }

        public void WriteNpcVisability(NpcVisablity value) =>
            Write((byte)value);

        public void WriteVector3(in Vector3 value)
        {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }

        public void WriteChatType(ChatType value) => Write((uint)value);

        public void WriteHeroId(Hero value) => Write((byte)value);

        public void WriteGateStatus(GateStatus value) => Write((byte)value);

        public PacketWriter(ClientOpcode opcode) : base(new MemoryStream(ushort.MaxValue), Encoding.ASCII, false)
        {
            /// Write SoulWorker magic bytes
            Write((byte)0x02);
            Write((byte)0x00);

            /// Write packet size (just reserve space, overwritten in GetBuffer)
            Write(ushort.MaxValue);

            /// I dont know
            Write((byte)0x1);

            /// Write packet opcode
            Write(ConvertUtils.LeToBeUInt16(opcode));
        }

        internal byte[] GetBuffer()
        {
            /// Skip SoulWorker magic bytes
            Seek(sizeof(ushort), SeekOrigin.Begin);

            /// Write actual packet size
            Write((ushort)BaseStream.Length);

            /// Get and return RAW buffer
            return ((MemoryStream)BaseStream).GetBuffer();
        }
    }
}