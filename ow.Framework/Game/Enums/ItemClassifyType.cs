using System;

namespace ow.Framework.Game.Enums
{
    [Flags]
    public enum ItemClassifyType
    {
        Generic = 0,
        GearWeapon = 1,
        GearSubWeapon = 2, /* Maybe */
        ClothesEarrings = 11,
        ClothesHeaddress = 12,
        ClothesGlasses = 13,
        ClothesMask = 14,
        ClothesHand = 15,
        ClothesUnderwear = 16,
        ClothesOuterwear = 17,
        ClothesWings = 18,
        ClothesSocks = 19,
        ClothesBots = 20,
        ClothesWeapon = 21,
        ClothesSkirt = 22,
        ClothesTail = 23,
        BroochAtack = 97,
        BroochDefence = 98,
        BroochEffect = 99,
        Unknown101 = 101,
        Unknown102 = 102,
        Unknown103 = 103,
        Unknown104 = 104,
        Unknown105 = 105,
        Unknown106 = 106,
        Unknown107 = 107,
        Unknown108 = 108,
        Unknown109 = 109,
        Unknown110 = 110,
        GearAmulet = 111,
        GearEarrings = 131,
        GearRing = 141,
        GearHead = 151,
        GearShoulder = 161,
        GearBody = 171,
        GearBots = 181,
        Consumable = 200,
        AkashicRecord = 201,
        BikeMuffler = 210,
        BikeTank = 211,
        Unknown240 = 240,
        Unknown241 = 241,
        Unknown242 = 242,
        Unknown243 = 243, // Npc Sphere
        AppearanceHair = 251,
        AppearanceHairColor = 252,
        AppearanceEyeColor = 253,
        AppearanceBodyColor = 254,
    }
}