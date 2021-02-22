using ow.Framework.Database.Storages;
using SoulCore;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal sealed partial record StoragesInfos
            {
                internal sealed partial class CostumeItems : List<CostumeItems.CostumeItem>
                {
                    internal CostumeItems(IEnumerable<ItemModel> value) : base(GetItems(value))
                    {
                    }

                    private static IEnumerable<CostumeItem> GetItems(IEnumerable<ItemModel> models)
                    {
                        CostumeItem[] items = Enumerable.Repeat(CostumeItem.Empty, Defines.EquipableFashionStorageMaxCapacity).ToArray();

                        foreach (ItemModel model in models)
                        {
                            items[model.Slot] = new CostumeItem(model);
                        }

                        return items;
                    }
                }
            }
        }
    }
}