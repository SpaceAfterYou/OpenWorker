using ow.Framework.Database.Storages;

namespace ow.Service.Login.Game.Gate
{
    internal sealed partial class CharacterList
    {
        internal sealed partial class Character
        {
            internal sealed partial record StoragesInfos
            {
                internal sealed partial class CostumeItems
                {
                    internal readonly struct CostumeItem
                    {
                        internal static readonly CostumeItem Empty = new();

                        internal readonly int PrototypeId;
                        internal readonly uint Color;

                        internal CostumeItem(ItemModel model)
                        {
                            PrototypeId = (int)model.PrototypeId;
                            Color = model.Color;
                        }
                    }
                }
            }
        }
    }
}