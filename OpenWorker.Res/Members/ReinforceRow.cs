namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What one upgrade costs: gold, ether, battle points and up to two materials with their counts. A material of zero is none.
/// </summary>
public readonly record struct ReinforceCost(int Gold, int Ether, int BattlePoint, int Material, short MaterialCount, int ExtraMaterial, short ExtraMaterialCount);

/// <summary>
/// What a row of <c>tb_Reinforce</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct ReinforceRow
{
    /// <summary>
    /// The cost of upgrading an item that is at <paramref name="level"/>, from 0 to 9; <c>null</c> past the levels of the row.
    /// </summary>
    public ReinforceCost? Cost(int level)
    {
        return level switch
        {
            0 => new(Gold01, Ether01, BattlePoint01, Material01, MaterialCount01, ExtraMaterial01, ExtraMaterialCount01),
            1 => new(Gold02, Ether02, BattlePoint02, Material02, MaterialCount02, ExtraMaterial02, ExtraMaterialCount02),
            2 => new(Gold03, Ether03, BattlePoint03, Material03, MaterialCount03, ExtraMaterial03, ExtraMaterialCount03),
            3 => new(Gold04, Ether04, BattlePoint04, Material04, MaterialCount04, ExtraMaterial04, ExtraMaterialCount04),
            4 => new(Gold05, Ether05, BattlePoint05, Material05, MaterialCount05, ExtraMaterial05, ExtraMaterialCount05),
            5 => new(Gold06, Ether06, BattlePoint06, Material06, MaterialCount06, ExtraMaterial06, ExtraMaterialCount06),
            6 => new(Gold07, Ether07, BattlePoint07, Material07, MaterialCount07, ExtraMaterial07, ExtraMaterialCount07),
            7 => new(Gold08, Ether08, BattlePoint08, Material08, MaterialCount08, ExtraMaterial08, ExtraMaterialCount08),
            8 => new(Gold09, Ether09, BattlePoint09, Material09, MaterialCount09, ExtraMaterial09, ExtraMaterialCount09),
            9 => new(Gold10, Ether10, BattlePoint10, Material10, MaterialCount10, ExtraMaterial10, ExtraMaterialCount10),
            _ => null
        };
    }
}
