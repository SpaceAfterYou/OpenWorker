using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;
using OpenWorker.Hotspot.Modules.ItemSetup.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The work on items through the handler of the district, against the tables of the client (OPENWORKER_TEST_RES) and, for an
/// upgrade, the server's (OPENWORKER_TEST_SERVER_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemSetupTest
{
    private static int FirstOfUse(ItemSetupFixture f, ItemUseType use, Func<ItemRow, bool>? also = null)
    {
        return ItemSetupFixture.Load<ItemRow>("tb_Item")
            .Where(e => f.Catalog.Classify(e.Id)?.ItemUseType == use && (also is null || also(e)))
            .Select(e => e.Id)
            .FirstOrDefault();
    }

    [TestMethod]
    public async Task RestoreMendsABrokenItemAndUnsealFreesABoundOneAsTheTableSays()
    {
        using var f = new ItemSetupFixture();

        // An item of the bag that may be restored and unsealed.
        var row = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.RestoreLimit > 0 && e.SealLimit > 0 && f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var target = f.Give(row.Id, 1);

        var restoring = f.Give(FirstOfUse(f, ItemUseType.WEAPON_RESTORE), 1);
        var unsealing = f.Give(FirstOfUse(f, ItemUseType.UNSEAL), 1);

        target.Flag = ItemRestoreService.BrokenFlag;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupRestoreRequest
        {
            Tool = ItemSetupFixture.SlotOf(restoring),
            Target = ItemSetupFixture.SlotOf(target)
        });

        Assert.AreEqual(0, target.Flag);
        Assert.AreEqual(1, target.RestoreCount);
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == restoring.Serial), "The restoring item is used up");

        target.BindType = ItemRestoreService.Bound;

        await f.Handler.OnHandleAsync(f.Context, new ItemSetupUnsealRequest
        {
            Tool = ItemSetupFixture.SlotOf(unsealing),
            Target = ItemSetupFixture.SlotOf(target)
        });

        Assert.AreEqual(ItemBindType.None, target.BindType);
        Assert.AreEqual(row.SealLimit == 255 ? 0 : 1, target.SealCount);
        Assert.IsFalse(f.Bag.Items.Any(e => e.Serial == unsealing.Serial), "The unsealing item is used up");
    }
}
