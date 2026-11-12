using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// One option of an item or of a gem: its type and its value. An item and a gem always carry
/// <see cref="ItemModuleDefines.OptionCount"/> of them.
/// </summary>
[WireMessage(WireMethods.Both)]
public readonly partial struct ItemOption
{
    public short Type { get; init; }
    public int Option { get; init; }

    public ItemOption(short type, int option)
    {
        Type = type;
        Option = option;
    }
}
