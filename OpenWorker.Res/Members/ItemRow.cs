namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a row of <c>tb_Item</c> gives beyond its columns, which are generated from <c>Structures.json</c>.
/// </summary>
public readonly partial struct ItemRow
{
    /// <summary>
    /// One of the five stat options of the item, from 0: its class (0 works on a stat, the others on a skill or a function), its
    /// effect and its value. An option with no effect ends the list.
    /// </summary>
    public (byte Kind, int Type, float Value) StatOption(int index)
    {
        return index switch
        {
            0 => (StatClass1, StatType1, StatValue1),
            1 => (StatClass2, StatType2, StatValue2),
            2 => (StatClass3, StatType3, StatValue3),
            3 => (StatClass4, StatType4, StatValue4),
            4 => (StatClass5, StatType5, StatValue5),
            _ => default
        };
    }

    /// <summary>
    /// The item id whose <c>tb_item_script</c> row supplies the name and icons: <see cref="ScriptItem"/> when set, otherwise
    /// <see cref="Id"/>. Most rows point at themselves; a few hundred share another item's script.
    /// </summary>
    public int ScriptOf => ScriptItem != 0 ? ScriptItem : Id;
}
