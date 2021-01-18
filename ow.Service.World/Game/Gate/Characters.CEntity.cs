using SoulCore.Database.Characters;
using SoulCore.Database.Storages;
using SoulCore.Game.Enums;

namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters
    {
        internal sealed partial class CEntity
        {
            internal int Id { get; }
            internal string Name { get; }
            internal uint Photo { get; }
            internal CEStoragesInfos Storages { get; }
            internal CEPlaceInfo Place { get; }
            internal byte Slot { get; set; }
            internal CharacterAdvancement Advancement { get; }
            internal Hero Hero { get; }
            internal byte Level { get; }
            internal CEAppearanceInfo Appearance { get; }

            internal CEntity(CharacterModel model, ItemContext context, BinTables tables)
            {
                Id = model.Id;
                Name = model.Name;
                Photo = model.Photo;
                Place = new(model.Place);
                Slot = model.Slot;
                Advancement = model.Advancement;
                Hero = model.Hero;
                Level = model.Level;
                Appearance = new()
                {
                    EquippedEyesColor = model.Appearance.EquippedEyeColor,
                    EquippedHair = new()
                    {
                        Color = model.Appearance.EquippedHair.Color,
                        Style = model.Appearance.EquippedHair.Style,
                    },
                    Hair = new()
                    {
                        Style = model.Appearance.Hair.Style,
                        Color = model.Appearance.Hair.Color,
                    },
                    EquippedSkinColor = model.Appearance.EquippedSkinColor,
                    EyesColor = model.Appearance.EyeColor,
                    SkinColor = model.Appearance.SkinColor,
                };

                Storages = new(model, context);
            }

            internal CEntity(CharacterModel model, ItemContext context)
            {
                Id = model.Id;
                Name = model.Name;
                Photo = model.Photo;
                Place = new(model.Place);
                Slot = model.Slot;
                Advancement = model.Advancement;
                Hero = model.Hero;
                Level = model.Level;
                Appearance = new()
                {
                    EquippedEyesColor = model.Appearance.EquippedEyeColor,
                    EquippedHair = new()
                    {
                        Color = model.Appearance.EquippedHair.Color,
                        Style = model.Appearance.EquippedHair.Style,
                    },
                    Hair = new()
                    {
                        Style = model.Appearance.Hair.Style,
                        Color = model.Appearance.Hair.Color,
                    },
                    EquippedSkinColor = model.Appearance.EquippedSkinColor,
                    EyesColor = model.Appearance.EyeColor,
                    SkinColor = model.Appearance.SkinColor,
                };

                Storages = new(model, context);
            }
        }
    }
}
