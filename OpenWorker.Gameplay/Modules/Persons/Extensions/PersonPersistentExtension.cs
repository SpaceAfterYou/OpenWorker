using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Gameplay.Modules.Persons.Extensions;

public static class PersonPersistentExtension
{
    public static TitleComponent ToTitleComponent(this PersonPersistent value)
    {
        return new TitleComponent
        {
            AbilityPrimary = value.TitleAbilityPrimary,
            AbilitySecondary = value.TitleAbilitySecondary,
            Primary = value.TitlePrimary,
            Secondary = value.TitleSecondary,
            Owned = value.TitleList
        };
    }
    
    public static CommunityComponent ToCommunityComponent(this PersonPersistent value)
    {
        return new CommunityComponent
        {
            State = (CommunityState)value.CommunityState,
            Comment = value.CommunityComment,
            Memo = value.CommunityMemo
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