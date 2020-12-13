namespace ow.Framework.Game
{
    public interface IReadOnlyItem
    {
        int Id { get; }
        int PrototypeId { get; }
        uint Color { get; }
    }
}