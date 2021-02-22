using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using SoulCore.Types;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal readonly int Id;
            internal readonly string Name;
            internal readonly uint Photo;
            internal readonly StoragesInfos Storages;
            internal readonly PlaceInfo Place;
            internal byte Slot { get; set; }
            internal readonly ClassAdvancement Advancement;
            internal readonly Class Class;
            internal readonly byte Level;
            internal readonly AppearanceInfo Appearance;

            internal Character(CharacterModel model, ItemContext context)
            {
                Id = model.Id;
                Name = model.Name;
                Photo = model.Photo;
                Place = new(model.Place);
                Slot = model.Slot;
                Advancement = model.Advancement;
                Class = model.Class;
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