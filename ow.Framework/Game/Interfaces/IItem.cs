namespace ow.Framework.Game
{
    public interface IItem
    {
        int Id { get; }
        int PrototypeId { get; }
        uint Color { get; }
        byte UpgradeLevel { get; }
    }
}