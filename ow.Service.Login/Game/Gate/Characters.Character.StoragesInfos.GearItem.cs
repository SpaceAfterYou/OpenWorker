using ow.Framework.Database.Storages;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal sealed partial record StoragesInfos
            {
                internal readonly struct GearItem
                {
                    internal static readonly GearItem Empty = new();

                    internal readonly int PrototypeId;
                    internal readonly byte UpgradeLevel;

                    internal GearItem(ItemModel model)
                    {
                        PrototypeId = (int)model.PrototypeId;
                        UpgradeLevel = model.Upgrade.CurrentLevel;
                    }
                }
            }
        }
    }
}