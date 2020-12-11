namespace ow.Framework.Game
{
    internal interface IReadOnlyItem
    {
        int Id { get; }
        int PrototypeId { get; }
    }
}