using ow.Framework.IO.Network.Responses.Shared;
using ow.Service.District.Network;
using System.Linq;

using static ow.Framework.IO.Network.Responses.Shared.CharacterShared.EquippedItemsInfo;

namespace ow.Service.District.Game.Helpers
{
    public static class ResponseHelper
    {
        internal static CharacterShared GetCharacter(Session session) => new()
        {
            Id = session.Character.Id,
            Name = session.Character.Name,
            Advancement = session.Character.Advancement,
            Hero = session.Character.Hero,
            Level = session.Character.Level,
            Photo = session.Character.Photo,
            Stats = new()
            {
                MaxStamina = (uint)session.Stats.Stamina.Value,
                CurrentHp = (uint)session.Stats.MaxHp.Value,
                MaxHp = (uint)session.Stats.MaxHp.Value,
                CurrentSg = (uint)session.Stats.CurrentSg.Value,
                MaxSg = (uint)session.Stats.MaxSg.Value,
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
            WeaponItem = new()
            {
                PrototypeId = session.Storages.EquippedGear.Weapon.PrototypeId,
                UpgradeLevel = session.Storages.EquippedGear.Weapon.UpgradeLevel,
            },
            EquippedItems = new()
            {
                Battle = session.Storages.EquippedBattleFashion.Select(s => new ItemInfo() { Color = s.Color, PrototypeId = s.PrototypeId }),
                View = session.Storages.EquippedViewFashion.Select(s => new ItemInfo() { Color = s.Color, PrototypeId = s.PrototypeId }),
            }
        };

        internal static PlaceShared GetPlace(Session session, Instance instance) => new()
        {
            Location = instance.Location.Id,
            Rotation = session.Character.Place.Rotation,
            Position = session.Character.Place.Position
        };
    }
}