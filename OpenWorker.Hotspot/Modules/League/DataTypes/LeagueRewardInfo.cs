namespace OpenWorker.Hotspot.Modules.League.DataTypes;

public readonly struct LeagueRewardInfo
{
    public LeagueRewardInfo()
    {
        Auth = 0;
        LimitGoldOut = 0;
    }
    
    public LeagueRewardInfo(BinaryReader reader)
    {
        Auth = reader.ReadInt32();
        LimitGoldOut = reader.ReadInt32();
    }

    public int Auth { get; init; }
    public int LimitGoldOut { get; init; }
}