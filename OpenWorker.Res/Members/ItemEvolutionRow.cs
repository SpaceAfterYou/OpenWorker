namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a row of <c>tb_Item_Evolution</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct ItemEvolutionRow
{
    /// <summary>
    /// The six items an evolution may give, each with its chance out of ten thousand; an item of zero is none.
    /// </summary>
    public (int Item, short Rate)[] Results =>
    [
        (Result1, ResultRate1),
        (Result2, ResultRate2),
        (Result3, ResultRate3),
        (Result4, ResultRate4),
        (Result5, ResultRate5),
        (Result6, ResultRate6)
    ];
}
