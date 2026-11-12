using System.Text;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Login.Responses;
using OpenWorker.Hotspot.Modules.Login.Types;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.World.Requests;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class LoginWireFormatTest
{
    private static byte[] ToBytes(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static BinaryReader ToReader(Action<BinaryWriter> write)
    {
        return new BinaryReader(new MemoryStream(ToBytes(write)));
    }

    private static ServerContent CreateContent()
    {
        return new ServerContent([]);
    }

    [TestMethod]
    public void EquipItemReadsUpgradeBeforeId()
    {
        using var reader = ToReader(writer =>
        {
            writer.Write((byte)9);
            writer.Write(1000);
        });

        var entry = new EquipItemValueEntry(reader);

        Assert.AreEqual(9, entry.Upgrade);
        Assert.AreEqual(1000, entry.Id);
    }

    [TestMethod]
    public void NextHumanNetworkFieldsStopAtTerminator()
    {
        var mac = new byte[18];
        Encoding.ASCII.GetBytes("AA-BB-CC-DD-EE-F").CopyTo(mac, 0);
        mac[17] = 0x7F; // Stale byte after the terminator.

        using var reader = ToReader(writer =>
        {
            writer.Write(new byte[2048]);
            writer.Write(new byte[4096]);
            writer.Write(new byte[21]);
            writer.Write(mac);
        });

        var request = new LoginNextHumanNetworkRequest(reader);

        Assert.AreEqual("AA-BB-CC-DD-EE-F", request.MacAddress);
        Assert.AreEqual(string.Empty, request.PurpleTicket);
    }

    [TestMethod]
    public void LongestIpAddressFitsGateList()
    {
        const string address = "255.255.255.255";

        var bytes = ToBytes(new LoginGateListResponse
        {
            Values =
            [
                new GateDataInfo
                {
                    Gate = new GateInfo
                    {
                        Id = 1,
                        Name = "G",
                        Workload = default,
                        Address = address,
                        Port = 1,
                        OnlineCount = 0
                    },
                    Person = new GatePersonInfo { Count = 0 }
                }
            ]
        }.Write);

        // previous, count, id, port, name (length, "G", terminator).
        const int lengthOffset = 1 + 1 + 2 + 2 + 2 + 2;

        Assert.AreEqual(address.Length, BitConverter.ToInt16(bytes, lengthOffset));
        Assert.IsLessThan(16, address.Length);
    }

    [TestMethod]
    public void XignCodeReadsFixedBlockAndString()
    {
        using var reader = ToReader(writer =>
        {
            writer.Write(7);
            writer.Write(Enumerable.Repeat((byte)0xFF, 512).ToArray());
            writer.Write((short)3);
            writer.Write("abc"u8.ToArray());
        });

        var request = new SystemXignCodeRequest(reader);

        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        Assert.AreEqual(7, request.Session);
        Assert.AreEqual(512, request.Buffer.Length);
        Assert.AreEqual("abc", request.Text);
    }

    [TestMethod]
    public void ServerContentHasAllEntriesAndAcceptsStoredValues()
    {
        var content = new ServerContent(
        [
            new ServerContentPersistent { Index = ServerContentIndex.CashShop, State = true }
        ]);

        Assert.HasCount(5, content);
        Assert.IsTrue(content[ServerContentIndex.CashShop]);
        Assert.IsFalse(content[ServerContentIndex.Attendance]);
    }

    [TestMethod]
    public void OptionLoadAlwaysHasClientSize()
    {
        var bytes = ToBytes(new LoginOptionLoadResponse { PersonOptions = [], Contents = CreateContent() }.Write);

        Assert.HasCount(LoginModuleDefines.PersonOptionCount + 5, bytes);
    }

    [TestMethod]
    public void ServerOptionUpdateHasFiveBytes()
    {
        var bytes = ToBytes(new SystemServerOptionUpdateResponse { Contents = CreateContent() }.Write);

        Assert.HasCount(5, bytes);
    }

    [TestMethod]
    public void AccountIdIsAscii()
    {
        var bytes = ToBytes(writer => new CharacterInfoGatePayload { AccountId = "abc" }.Write(writer));

        // int + long + 4 bytes + 2 uint + 3 long precede the string.
        const int offset = 4 + 8 + 4 + 8 + 24;

        Assert.AreEqual(4, BitConverter.ToInt16(bytes, offset));
        Assert.AreEqual("abc\0", Encoding.ASCII.GetString(bytes, offset + 2, 4));
    }

    [TestMethod]
    public void WorldEnterRequestMatchesClient()
    {
        using var reader = ToReader(writer =>
        {
            writer.Write(1);
            writer.Write(2);
            writer.Write(3);
            writer.Write((short)4);
            writer.Write((short)5);
            writer.Write(6);
            writer.Write(7);
            writer.Write(8L);
            writer.Write((short)9);
            writer.Write((byte)1);
        });

        var request = new WorldEnterRequest(reader);

        Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        Assert.AreEqual(1, request.Account);
        Assert.AreEqual(3, request.Party);
        Assert.AreEqual(4, request.Location);
        Assert.AreEqual(7, request.Portal);
        Assert.AreEqual(9, request.DistrictWorld);
    }
}
