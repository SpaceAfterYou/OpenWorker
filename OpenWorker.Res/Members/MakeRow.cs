namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a recipe of <c>tb_Make</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct MakeRow
{
    /// <summary>
    /// The four materials of one item made and how many of each, a material of zero being none.
    /// </summary>
    public (int Item, short Count)[] Materials =>
    [
        (Material1, MaterialCount1),
        (Material2, MaterialCount2),
        (Material3, MaterialCount3),
        (Material4, MaterialCount4)
    ];
}
