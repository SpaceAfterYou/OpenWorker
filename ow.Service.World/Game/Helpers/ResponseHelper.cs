using ow.Service.District.Game.Storage;
using ow.Service.District.Network.Sync;
using SoulCore.IO.Network.PacketSharedStructure;
using System.Linq;

namespace ow.Service.District.Game.Helpers
{
    public static class ResponseHelper
    {
        public static CharacterExPacketSharedStructure GetCharacterExPacket(SyncSession session) => new()
        {
            Character = GetCharacter(session),
            Position = GetPosition(session)
        };

        private static CharacterPacketSharedStructure GetCharacter(SyncSession session) => new()
        {
            Id = session.Character.Id,
            Name = session.Character.Name,
            Advancement = session.Character.Advancement,
            Class = session.Character.Class,
            Level = session.Character.Level,
            Photo = session.Character.Photo,
            Stats = new()
            {
                Hp = new()
                {
                    Current = (uint)session.Stats.CurrentHp.Value,
                    Max = (uint)session.Stats.MaxHp.Value
                },
                Sg = new()
                {
                    Current = (uint)session.Stats.CurrentSg.Value,
                    Max = (uint)session.Stats.MaxSg.Value
                },
                Stamina = new()
                {
                    Current = (uint)session.Stats.Stamina.Value,
                    Max = (uint)session.Stats.Stamina.Value
                },
                AttackSpeed = session.Stats.AttackSpeed.Value,
                MoveSpeed = session.Stats.MoveSpeed.Value,
            },
            Appearance = new()
            {
                EquippedEyeColor = session.Character.Appearance.EquippedEyeColor,
                EquippedHair = new()
                {
                    Color = session.Character.Appearance.EquippedHair.Color,
                    Style = session.Character.Appearance.EquippedHair.Style,
                },
                Hair = new()
                {
                    Style = session.Character.Appearance.Hair.Style,
                    Color = session.Character.Appearance.Hair.Color,
                },
                EquippedSkinColor = session.Character.Appearance.EquippedSkinColor,
                EyeColor = session.Character.Appearance.EyeColor,
                SkinColor = session.Character.Appearance.SkinColor,
            },
            WeaponItem = GetGearItemInfo(session.Storages.EquippedGear.Weapon),
            CostumesItems = new()
            {
                Battle = session.Storages.EquippedBattleFashion.Select(s => GetFashionItemInfo(s)),
                View = session.Storages.EquippedViewFashion.Select(s => GetFashionItemInfo(s)),
            },
        };

        private static CharacterPacketSharedStructure.GearInfo GetGearItemInfo(StorageItem? item)
        {
            if (item is not null)
            {
                return new()
                {
                    PrototypeId = (int)item.Prototype.Id,
                    UpgradeLevel = item.Upgrade.CurrentLevel,
                };
            }

            return CharacterPacketSharedStructure.GearInfo.Empty;
        }

        private static CharacterPacketSharedStructure.CostumeInfos.CostumeInfo GetFashionItemInfo(StorageItem? item)
        {
            if (item is not null)
            {
                return new()
                {
                    PrototypeId = (int)item.Prototype.Id,
                    Color = item.Color,
                };
            }

            return CharacterPacketSharedStructure.CostumeInfos.CostumeInfo.Empty;
        }

        public static PositionInfoPacketSharedStructure GetPosition(SyncSession session) => new()
        {
            LocationId = session.Server.Instance.LocationId,
            MapId = new(session.Channel.Id),
            Position = session.Character.Place.Position,
            Rotation = session.Character.Place.Yaw,
        };
    }
}