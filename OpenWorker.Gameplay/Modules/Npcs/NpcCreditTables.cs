using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// What a grade of credit with a group of NPCs gives, as <c>tb_Customer_Benefit</c> names it.
/// </summary>
public enum CreditBenefit : byte
{
    /// <summary>A part of the price of a repair, out of ten thousand.</summary>
    RepairMoney = 1,

    /// <summary>A part of the gold of production, out of ten thousand.</summary>
    MakeMoney = 2,

    /// <summary>A part of the materials of production past the first, out of ten thousand.</summary>
    MakeMaterials = 3,

    /// <summary>A part of the gold of an upgrade, out of ten thousand.</summary>
    UpgradeMoney = 4,

    /// <summary>Added to the chance of an upgrade, out of ten thousand.</summary>
    UpgradeChance = 5,

    /// <summary>A part of the gold of a transport, out of ten thousand.</summary>
    TransportMoney = 6,

    /// <summary>The items of the benefit may be bought at a shop that sells them from this grade.</summary>
    GradeItems = 7,

    /// <summary>More of the items of the benefit may be bought in a day.</summary>
    GradeItemCount = 8
}

/// <summary>
/// What raises the credit of a group of NPCs, as <c>tb_Credit_Raise</c> names it.
/// </summary>
public enum CreditRaise : byte
{
    Buy = 1,
    Repair = 2,
    Make = 3,
    Upgrade = 4,
    Exchange = 5,
    Evolution = 6,
    SocketRemove = 7,
    SocketAdd = 8,
    Transport = 9,
    Renovate = 10,
    Refine = 11
}

/// <summary>
/// The tables of NPC credit. A row of <c>tb_Customer_Grade</c> and of <c>tb_Credit_Raise</c> is found by ten times the
/// group of NPCs plus the grade or the kind of raise.
/// </summary>
public sealed class NpcCreditTables(
    ReadOnlyCollection<CustomerGradeRow> grades,
    ReadOnlyCollection<CustomerBenefitRow> benefits,
    ReadOnlyCollection<CreditRaiseRow> raises)
{
    /// <summary>
    /// The highest grade: past it a group gives no more credit.
    /// </summary>
    public const byte MaxGrade = 5;

    private readonly RowIndex<int, CustomerGradeRow> _grades = grades.IndexBy(e => (int)e.Id);
    private readonly RowIndex<int, CustomerBenefitRow> _benefits = benefits.IndexBy(e => e.Id);
    private readonly RowIndex<int, CreditRaiseRow> _raises = raises.IndexBy(e => (int)e.Id);

    /// <summary>
    /// The row of a grade of a group: what reaching it needs and gives, and its benefits.
    /// </summary>
    public bool TryGrade(int group, int grade, out CustomerGradeRow row) => _grades.TryGet(group * 10 + grade, out row);

    /// <summary>
    /// How much credit an action of a kind gives with a group, from the least to the most.
    /// </summary>
    public bool TryRaise(int group, CreditRaise raise, out CreditRaiseRow row) => _raises.TryGet(group * 10 + (int)raise, out row);

    /// <summary>
    /// The benefits of a grade of a group, as their rows.
    /// </summary>
    public IEnumerable<CustomerBenefitRow> BenefitsOf(int group, int grade)
    {
        if (grade <= 0 || !TryGrade(group, grade, out var row))
        {
            yield break;
        }

        foreach (var id in (int[])[row.Benefit1, row.Benefit2, row.Benefit3, row.Benefit4, row.Benefit5, row.Benefit6])
        {
            if (id == 0)
            {
                yield break;
            }

            if (_benefits.TryGet(id, out var benefit))
            {
                yield return benefit;
            }
        }
    }
}
