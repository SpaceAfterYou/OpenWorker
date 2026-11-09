using System.Buffers.Binary;
using System.Collections.ObjectModel;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.Server.Common.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the others see of a player's gear, against the tables of the client: set OPENWORKER_TEST_RES to the folder with the
/// *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class WornGearTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

    // A second weapon the first class can wear.
    private const int SecondWeapon = 521;

    private static ReadOnlyCollection<T> Load<T>(string table)
    {
        var folder = Environment.GetEnvironmentVariable(TablesVariable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{TablesVariable} is not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Combine(folder, table + ".res")));

        var count = reader.ReadInt32();

        return new ReadOnlyCollection<T>(Enumerable.Range(0, count).Select(_ => (T)Activator.CreateInstance(typeof(T), reader)!).ToArray());
    }

    /// <summary>
    /// A player who puts on a weapon is seen with it by the players who see it, from the notice the client changes the model
    /// of another player with: the actor, the worn slot and the item now in it. A player out of sight is not told (it learns
    /// the gear from the record of the person appearing, built from the bag when it comes into sight), nor is the wearer.
    /// </summary>
    [TestMethod]
    public async Task AChangeOfGearReachesThoseWhoSeeThePlayerOnly()
    {
        var catalog = new ItemCatalog(
            Load<ItemRow>("tb_Item"),
            Load<ItemClassifyRow>("tb_Item_Classify"),
            Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
            Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

        var serials = new LocalItemSerials();
        var factory = new InventoryFactory(catalog, Load<CreateClothRow>("tb_Create_Cloth"), serials);

        using var district = new DistrictHarness();

        var inventory = new InventoryService(district.Ecs, catalog, serials);
        var items = new ItemService(district.Ecs, inventory, null!, new ChannelAudience(district.Ecs, district.Channels));

        var wearer = district.Join(100f, 100f);
        var near = district.Join(WorldOptionsCell * 1.5f, 100f);
        var far = district.Join(WorldOptionsCell * 4f, 100f);

        district.Ecs.Set(wearer.Entity, new PersonInfoComponent { Name = "Wearer", Hero = Hero.Haru });
        district.Ecs.Set(wearer.Entity, new LevelComponent(1, 0));
        district.Ecs.Set(wearer.Entity, new InventoryComponent(factory.CreateNew(new PersonPersistent
        {
            Id = 7,
            Name = "Wearer",
            Hero = Hero.Haru,
            DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
            EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
            Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
            Account = null!, Gate = null!
        })));

        Assert.IsTrue(inventory.TryGive(wearer.Entity, SecondWeapon, 1));

        var bag = inventory.InventoryOf(wearer.Entity)!;
        var spare = bag.Find(StorageGroup.Common)!.Slots.OfType<ItemInstance>().Single();
        var worn = bag.At(StorageGroup.AbilityEquip, 0)!;

        await district.ResetAsync(wearer, near, far);

        await items.OnHandleAsync(new ServiceHandleContext(wearer.Entity, CancellationToken.None), Move(
            (StorageGroup.Common, SecondWeapon, (short)spare.Slot),
            (StorageGroup.AbilityEquip, worn.Item, 0)));

        await district.SettleAsync(wearer, near, far);

        var notice = near.Received().Single(e => e.Is(GroupOpcode.Item, (byte)ItemOpcode.MoveBt)).Body;

        Assert.AreEqual(wearer.Actor, new Domain.Components.ActorComponent(BinaryPrimitives.ReadInt32LittleEndian(notice)).Identifier);
        Assert.AreEqual(1, BinaryPrimitives.ReadInt32LittleEndian(notice.AsSpan(4)), "One worn slot changed.");
        Assert.AreEqual((byte)StorageGroup.AbilityEquip, notice[8]);
        Assert.AreEqual(0, BinaryPrimitives.ReadInt16LittleEndian(notice.AsSpan(9)));
        Assert.AreEqual(SecondWeapon, BinaryPrimitives.ReadInt32LittleEndian(notice.AsSpan(11)), "The slot holds the new weapon.");

        Assert.IsFalse(far.Received().Any(e => e.Is(GroupOpcode.Item, (byte)ItemOpcode.MoveBt)), "A player out of sight is not told.");
        Assert.IsFalse(wearer.Received().Any(e => e.Is(GroupOpcode.Item, (byte)ItemOpcode.MoveBt)), "The wearer has its own answer.");
    }

    private const float WorldOptionsCell = OpenWorker.Extensions.WorldOptions.DefaultVisibilityRadius;

    private static ItemMoveRequest Move((StorageGroup Storage, int Item, short Slot) src, (StorageGroup Storage, int Item, short Slot) dest)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        foreach (var (storage, item, slot) in new[] { src, dest })
        {
            writer.Write((byte)storage);
            writer.Write(item);
            writer.Write(slot);
        }

        writer.Flush();
        stream.Position = 0;

        return new ItemMoveRequest(new BinaryReader(stream));
    }
}
