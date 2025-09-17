using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Persons.Components;

namespace OpenWorker.Hotspot.Modules.Persons.Extensions;

public static class PersonPersistentExtension
{
    public static TitleComponent ToTitleComponent(this PersonPersistent value)
    {
        return new TitleComponent
        {
            Primary = value.TitlePrimary,
            Secondary = value.TitleSecondary
        };
    }
    
    public static AppearanceComponent ToAppearanceComponent(this PersonPersistent value)
    {
        return new AppearanceComponent
        {
            HairStyle = new AppearanceComponentEntry
            {
                Shape = value.DefaultHairStyle,
                Look = value.EquippedHairStyle
            },
            HairColor = new AppearanceComponentEntry
            {
                Shape = value.DefaultHairColor,
                Look = value.EquippedHairColor
            },
            EyeColor = new AppearanceComponentEntry
            {
                Shape = value.DefaultEyeColor,
                Look = value.EquippedEyeColor
            },
            SkinColor = new AppearanceComponentEntry
            {
                Shape = value.DefaultSkinColor,
                Look = value.EquippedSkinColor
            }
        };
    }
}