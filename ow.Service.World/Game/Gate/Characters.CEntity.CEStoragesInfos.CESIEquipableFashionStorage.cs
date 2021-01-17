using ow.Framework;
using ow.Framework.Database.Storages;
using System.Collections.Generic;
using System.Linq;

namespace ow.Service.World.Game.Gate
{
    internal sealed partial class Characters
    {
        internal sealed partial class CEntity
        {

            internal sealed partial record CEStoragesInfos
            {
                internal sealed class CESIEquipableFashionStorage : List<CESIEquipableFashionStorage.Item?>
                {
                    internal sealed record Item
                    {
                        internal int PrototypeId { get; init; }
                        internal uint Color { get; init; }
                    }

                    internal CESIEquipableFashionStorage(IEnumerable<ItemModel> value) : base(GetItems(value))
                    {
                    }

                    private static IEnumerable<Item?> GetItems(IEnumerable<ItemModel> value)
                    {
                        Item?[] items = Enumerable.Repeat((Item?)null, Defines.EquipableFashionStorageMaxCapacity).ToArray();

                        foreach (ItemModel model in value)
                            items[model.Slot] = new Item()
                            {
                                PrototypeId = (int)model.PrototypeId,
                                Color = model.Color
                            };

                        return items;
                    }
                }
            }
        }
    }
}