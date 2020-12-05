using System;
using System.Xml;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;
using TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game.World.Batch;

namespace TrinigyVisionEngine.Vision.Runtime.EnginePlugins.Game
{
    [Flags]
    public enum SlotType
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

    //public enum EquipSlotType
    //{
    //    Unknown1 = 1,
    //    Unknown2 = 2,
    //    Unknown11 = 11,
    //    Unknown12 = 12,
    //    Unknown13 = 13,
    //    Unknown14 = 14,
    //    Unknown15 = 15,
    //    Unknown16 = 16,
    //    Unknown17 = 17,
    //    Unknown18 = 18,
    //    Unknown19 = 19,
    //    Unknown20 = 20,
    //    Unknown21 = 21,
    //    Unknown22 = 22,
    //    Unknown23 = 23,
    //}

    public static class GamePlugInSceneManager
    {
        public static Root LoadBatch(string archiveName, string fileName)
        {
            using var archive = new VArchive(archiveName);
            using var stream = archive[fileName].OpenReader();

            var xml = new XmlDocument();
            xml.Load(stream);

            var root = xml.DocumentElement;
            return new Root(root);
        }

        //public static SlotType GenerateItemType(EquipSlotType type) =>
        //    type switch
        //    {
        //        EquipSlotType.Unknown1 => SlotType.WeaponGear,
        //        EquipSlotType.Unknown2 => SlotType.Unknown1,
        //        EquipSlotType.Unknown11 => SlotType.EarringsClothes,
        //        EquipSlotType.Unknown12 => SlotType.HeaddressClothes,
        //        EquipSlotType.Unknown13 => SlotType.GlassesClothes,
        //        EquipSlotType.Unknown14 => SlotType.MaskClothes,
        //        EquipSlotType.Unknown15 => SlotType.HandClothes,
        //        EquipSlotType.Unknown16 => SlotType.UnderwearClothes,
        //        EquipSlotType.Unknown17 => SlotType.OuterwearClothes,
        //        EquipSlotType.Unknown18 => SlotType.WingsClothes,
        //        EquipSlotType.Unknown19 => SlotType.SocksClothes,
        //        EquipSlotType.Unknown20 => SlotType.ClothesBots,
        //        EquipSlotType.Unknown21 => SlotType.WeaponFashion,
        //        EquipSlotType.Unknown22 => SlotType.SkirtClothes,
        //        EquipSlotType.Unknown23 => SlotType.TailClothes,
        //        _ => throw new InvalidEnumArgumentException()
        //    };
    }
}
