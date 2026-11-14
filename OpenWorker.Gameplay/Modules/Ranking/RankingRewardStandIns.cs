using System.Collections.Frozen;
using OpenWorker.Gameplay.Modules.Items;

namespace OpenWorker.Gameplay.Modules.Ranking;

/// <summary>
/// Real items given in place of the ranking rewards the client's item table lacks. The reward table names HP potions of
/// levels 1 to 55 that the client has a name and an icon for but no item row, so it neither shows them in a letter nor
/// takes them into the bag. Each is given as the strongest medibox (HP potion) whose level limit is not above the potion's
/// level: our choice, the client names no replacement.
/// </summary>
public static class RankingRewardStandIns
{
    private const int BitMedibox = 610101001;
    private const int KiloMedibox = 610101002;
    private const int MegaMedibox = 610101003;
    private const int GigaMedibox = 610101004;
    private const int TeraMedibox = 610101005;
    private const int PetaMedibox = 610101006;

    private static readonly FrozenDictionary<int, int> StandIns = new Dictionary<int, int>
    {
        [610901000] = BitMedibox, // level 1
        [610901001] = BitMedibox, // level 8
        [610901002] = KiloMedibox, // level 15
        [610901003] = KiloMedibox, // level 20
        [610901004] = MegaMedibox, // level 26
        [610901005] = MegaMedibox, // level 33
        [610901006] = GigaMedibox, // level 41
        [610901007] = TeraMedibox, // level 48
        [610901008] = PetaMedibox // level 55
    }.ToFrozenDictionary();

    /// <summary>
    /// The item a reward is given as: the reward itself when the item table has it, else its stand-in when the table has
    /// that; 0 when neither.
    /// </summary>
    public static int Resolve(int item, ItemCatalog catalog)
    {
        if (catalog.Exists(item))
        {
            return item;
        }

        return StandIns.TryGetValue(item, out var standIn) && catalog.Exists(standIn) ? standIn : 0;
    }
}
