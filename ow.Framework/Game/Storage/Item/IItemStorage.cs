namespace ow.Framework.Game.Storage.Item
{
    public interface IItemStorage
    {
        int Id { get; }
        int PrototypeId { get; }
        uint Color { get; }
        byte UpgradeLevel { get; }
    }
}