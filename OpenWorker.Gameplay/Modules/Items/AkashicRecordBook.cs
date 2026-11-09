using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// A card in the akashic record book: the card, its place in the book (one card per place, from the card's row of
/// <c>tb_Akashic_Records</c>) and the experience it had when it went in, which it takes back when it comes out.
/// </summary>
public sealed class AkashicRecord
{
    public int Card { get; set; }

    public int Position { get; set; }

    public int Exp { get; set; }
}

/// <summary>
/// The akashic record book of a person: the cards it has put into the book from its bag. It is kept with the items and
/// written as a document of the person (<c>PersonStateKind.AkashicRecords</c>) when <c>PersonSections.AkashicRecords</c> is
/// marked.
/// </summary>
public sealed class AkashicRecordBook
{
    /// <summary>
    /// The book as the flush of the person writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } = new PersonDocumentSource<InventoryComponent>(
        PersonStateKind.AkashicRecords, PersonSections.AkashicRecords, e => e.Value?.AkashicRecords);

    public List<AkashicRecord> Records { get; set; } = [];

    public AkashicRecord? At(int position)
    {
        return Records.Find(e => e.Position == position);
    }

    /// <summary>
    /// The book as the client reads it.
    /// </summary>
    public AkashicRecordValue[] Values()
    {
        return Records.Select(e => new AkashicRecordValue { Card = e.Card, Position = e.Position }).ToArray();
    }
}
