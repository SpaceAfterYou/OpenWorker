using SoulCore.Database.Storages;

namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters
    {
        internal sealed partial class CEntity
        {
            internal sealed partial record CEStoragesInfos
            {
                internal sealed record CESIEquipableGearStorageItem
                {
                    internal int PrototypeId { get; init; }
                    internal byte UpgradeLevel { get; init; }

                    internal static CESIEquipableGearStorageItem Empty { get; } = new();

                    internal CESIEquipableGearStorageItem(ItemModel model)
                    {
                        PrototypeId = (int)model.PrototypeId;
                        UpgradeLevel = model.Upgrade.CurrentLevel;
                    }

                    private CESIEquipableGearStorageItem()
                    {
                    }
                }
            }
        }
    }
}
