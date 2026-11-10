namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a row of <c>tb_item_script</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct ItemScriptRow
{
    /// <summary>
    /// Alternate icon paths (<see cref="Icon2"/>…<see cref="Icon6"/>). On client 1.4.2.3 almost always empty or a copy of
    /// <see cref="Icon"/>; a few costume rows repeat the same path in <see cref="Icon2"/>.
    /// </summary>
    public string[] Icons => [Icon, Icon2, Icon3, Icon4, Icon5, Icon6];

    /// <summary>
    /// The five trailing bytes after the icon strings: <see cref="Part"/> then <see cref="Param1"/>…<see cref="Param4"/>.
    /// <see cref="Part"/> clusters with costume classify groups when non-zero; the params are unused on almost every row.
    /// </summary>
    public byte[] Params => [Part, Param1, Param2, Param3, Param4];
}
