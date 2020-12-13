namespace ow.Framework.Game
{
    internal class Item : IReadOnlyItem
    {
        public int Id { get; }
        public int PrototypeId { get; }
        public uint Color { get; }
    }
}