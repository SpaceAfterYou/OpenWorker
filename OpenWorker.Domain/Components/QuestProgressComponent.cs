namespace OpenWorker.Domain.Components;

public static class QuestProgressDefines
{
    public const int MaxActiveEpisodes = 8;
    public const int ConditionsPerEpisode = 10;
}

public struct QuestConditionStepBlock
{
    public byte S0, S1, S2, S3, S4, S5, S6, S7, S8, S9;

    public readonly byte Get(int index) => index switch
    {
        0 => S0,
        1 => S1,
        2 => S2,
        3 => S3,
        4 => S4,
        5 => S5,
        6 => S6,
        7 => S7,
        8 => S8,
        9 => S9,
        _ => 0
    };

    public void Set(int index, byte value)
    {
        switch (index)
        {
            case 0: S0 = value; break;
            case 1: S1 = value; break;
            case 2: S2 = value; break;
            case 3: S3 = value; break;
            case 4: S4 = value; break;
            case 5: S5 = value; break;
            case 6: S6 = value; break;
            case 7: S7 = value; break;
            case 8: S8 = value; break;
            case 9: S9 = value; break;
        }
    }
}

public struct ActiveQuestEpisodeState
{
    public int EpisodeId;
    public byte IsAddHelper;
    public byte IsFailed;
    public QuestConditionStepBlock Steps;
}

public struct QuestProgressComponent
{
    public byte ActiveCount { get; set; }
    public ActiveQuestEpisodeState E0 { get; set; }
    public ActiveQuestEpisodeState E1 { get; set; }
    public ActiveQuestEpisodeState E2 { get; set; }
    public ActiveQuestEpisodeState E3 { get; set; }
    public ActiveQuestEpisodeState E4 { get; set; }
    public ActiveQuestEpisodeState E5 { get; set; }
    public ActiveQuestEpisodeState E6 { get; set; }
    public ActiveQuestEpisodeState E7 { get; set; }
}
