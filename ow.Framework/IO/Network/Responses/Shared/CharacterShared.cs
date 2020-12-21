using ow.Framework.Game.Enums;
using System.Collections.Generic;

namespace ow.Framework.IO.Network.Responses.Shared
{
    public record CharacterShared
    {
        public readonly struct StatsInfo
        {
            public uint MaxStamina { get; init; }
            public uint CurrentHp { get; init; }
            public uint MaxHp { get; init; }
            public uint CurrentSg { get; init; }
            public uint MaxSg { get; init; }
            public float AttackSpeed { get; init; }
            public float MoveSpeed { get; init; }
        }

        public readonly struct AppearanceInfo
        {
            public readonly struct HairInfo
            {
                public ushort Style { get; init; }
                public ushort Color { get; init; }
            }

            public HairInfo Hair { get; init; }
            public ushort EyeColor { get; init; }
            public ushort SkinColor { get; init; }
            public HairInfo EquippedHair { get; init; }
            public ushort EquippedEyeColor { get; init; }
            public ushort EquippedSkinColor { get; init; }
        }

        public sealed record GearItemInfo
        {
            public byte UpgradeLevel { get; init; }
            public int PrototypeId { get; init; } = -1;
        }

        public readonly struct EquippedItemsInfo
        {
            public sealed record ItemInfo
            {
                public int PrototypeId { get; init; }
                public uint Color { get; init; }
            }

            public IEnumerable<ItemInfo?> View { get; init; }
            public IEnumerable<ItemInfo?> Battle { get; init; }
        }

        public int Id { get; init; }
        public byte Slot { get; init; }
        public byte Level { get; init; }
        public Hero Hero { get; init; }
        public CharacterAdvancement Advancement { get; init; }
        public uint Photo { get; init; }
        public string Name { get; init; } = default!;
        public AppearanceInfo Appearance { get; init; }
        public StatsInfo Stats { get; init; }
        public GearItemInfo WeaponItem { get; init; } = default!;
        public EquippedItemsInfo EquippedItems { get; init; }
    }
}