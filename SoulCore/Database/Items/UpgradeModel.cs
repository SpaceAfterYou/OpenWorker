namespace SoulCore.Database.Storages
{
    public sealed class UpgradeModel
    {
        public byte UsedAttempts { get; init; }
        public byte CurrentLevel { get; init; }
    }
}
