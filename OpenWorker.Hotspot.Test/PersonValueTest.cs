using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class PersonValueTest
{
    private World World { get; } = World.Create();

    [TestMethod]
    public void Test()
    {
        // using var entity = World.Create();
        //
        // var storageBuilder = new StorageBuilder(World);
        // var context = storageBuilder.CreateContext();
        //
        // context.Use(StorageType.ShapeEquip);
        // context.Use(StorageType.LookEquip);
        //
        // entity.Add(context.Build());
    }
}

[TestClass]
public sealed class MapValueTest
{
    [TestMethod]
    public void Test()
    {
        var raw = new MapValue
        {
            Location = 10003,
            Channel = 11,
            Server = 3
        };
        
        ulong packed = raw;
        MapValue result = packed;
        
        Assert.AreEqual(10003, result.Location);
        
        Assert.AreEqual((byte)11, result.Channel);
        Assert.AreEqual((short)11, result.Channel);
        
        Assert.AreEqual(3, result.Server);
    }
}