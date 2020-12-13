namespace ow.Framework.Game
{
    internal class Item : IItem
    {
        public int Id { get; }
        public int PrototypeId { get; }
        public uint Color { get; }
        public byte UpgradeLevel { get; }
    }
}