using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The size of the soul gauge of a status, from the server's own <c>tb_Status</c>. The client's table has the same rows
/// without this column, and the client is told the size with the stats of the player.
/// </summary>
[BinaryResourceTable("tb_Status", Server = true)]
public readonly struct StatusSoulGaugeRow
{
    // The six main stats with their growth, then the constants of the status, from the growth of life to the reduction of
    // magic damage.
    private const int Constants = 35;

    // The resistances to burns, poison, shocks, bleeding, stuns, paralysis, sleep, freezing, charm, confusion, silence and
    // weakness.
    private const int Resistances = 12;

    // The reduction of damage between players and its rate against defence, the damage to super armor, the share of the soul
    // gauge a hit gives, the first life, stamina and both accuracies, the damage of a miss and the check of super armor.
    private const int Rest = 10;

    public StatusSoulGaugeRow(BinaryReader reader)
    {
        Id = reader.ReadInt16();
        reader.ReadBytes(Constants * sizeof(float) + Resistances);
        SoulGauge = reader.ReadUInt16();
        reader.ReadBytes(Rest * sizeof(float));
    }

    public short Id { get; }

    /// <summary>
    /// How much soul gauge a character of the status holds, before what its gear adds.
    /// </summary>
    public ushort SoulGauge { get; }
}
