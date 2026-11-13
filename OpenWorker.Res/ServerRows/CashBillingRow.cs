using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The prices of a product of the cash shop, from the server's own <c>tb_CashBilling_Info</c>: up to five choices, each an
/// item, its count, the price before the sale and the price paid. The client's table has the same rows with every price 0.
/// </summary>
[BinaryResourceTable("tb_CashBilling_Info", Server = true)]
public readonly struct CashBillingRow
{
    public const int Choices = 5;

    public CashBillingRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();

        // The code of the first choice for the billing service.
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        Items = Read(reader);
        Counts = Read(reader);
        BasePrices = Read(reader);
        Prices = Read(reader);

        // The codes of the other four choices.
        for (var index = 1; index < Choices; index++)
        {
            reader.ReadUtf16UnicodeStringWithoutTerminator();
        }
    }

    public ushort Id { get; }

    public int[] Items { get; }

    public int[] Counts { get; }

    /// <summary>
    /// The prices before the sale, which the window shows struck through.
    /// </summary>
    public int[] BasePrices { get; }

    /// <summary>
    /// The prices paid.
    /// </summary>
    public int[] Prices { get; }

    private static int[] Read(BinaryReader reader)
    {
        var values = new int[Choices];

        for (var index = 0; index < Choices; index++)
        {
            values[index] = reader.ReadInt32();
        }

        return values;
    }
}
