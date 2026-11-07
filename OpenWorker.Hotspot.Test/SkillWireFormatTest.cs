using System.Numerics;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SkillWireFormatTest
{
    private static byte[] Written(IResponseHotspotMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static T Read<T>(byte[] data, Func<BinaryReader, T> create)
    {
        using var reader = new BinaryReader(new MemoryStream(data));

        var value = create(reader);

        Assert.AreEqual(data.Length, reader.BaseStream.Position, "the whole message is read");

        return value;
    }

    [TestMethod]
    public void ChainAnnouncementIsOwnerAndFortyTwoBytesAfterIt()
    {
        var data = Written(new SkillChainBroadcastResponse
        {
            Owner = new ActorValue(5, ActorType.User),
            SkillChainPosition = default(SkillChainPosition) with
            {
                Skill = 52000711,
                TriggerIndex = 105,
                Position = new Vector3(1, 2, 3),
                Direction = new Vector3(0, -1, 0),
                Session = 9,
                Target = new ActorValue(7, ActorType.Monster)
            }
        });

        Assert.HasCount(4 + 4 + 2 + 12 + 12 + 4 + 4, data);
        Assert.AreEqual(52000711, BitConverter.ToInt32(data, 4));
        Assert.AreEqual((short)105, BitConverter.ToInt16(data, 8));
        Assert.AreEqual(9u, BitConverter.ToUInt32(data, 34));

        var back = Read(data, e => new SkillChainBroadcastResponse(e));

        Assert.AreEqual(52000711, back.SkillChainPosition.Skill, "a skill id does not fit in a short");
        Assert.AreEqual(new Vector3(0, -1, 0), back.SkillChainPosition.Direction);
        Assert.AreEqual(7, back.SkillChainPosition.Target.Identifier);
    }

    [TestMethod]
    public void ChainWithoutAnyoneNamesNobody()
    {
        var data = Written(new SkillChainBroadcastResponse
        {
            SkillChainPosition = default(SkillChainPosition) with { Target = (ActorValue)(-1) }
        });

        Assert.AreEqual(-1, BitConverter.ToInt32(data, data.Length - 4));
    }

    [TestMethod]
    public void ChainTargetUpdateIsSessionFlagAndTarget()
    {
        var data = Written(new SkillChainUpdateTargetResponse { Session = 3, IsAdd = true, Target = new ActorValue(7, ActorType.Monster) });

        Assert.HasCount(4 + 1 + 4, data);
        Assert.AreEqual(3u, BitConverter.ToUInt32(data));
        Assert.AreEqual((byte)1, data[4]);
    }

    [TestMethod]
    public void ChainRequestIsThirtyEightBytes()
    {
        var request = default(SkillProjectileValue) with { Skill = 52000711, TriggerIndex = 105, Target = new ActorValue(7, ActorType.Monster) };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        request.Write(writer);
        writer.Flush();

        Assert.HasCount(38, stream.ToArray());

        var back = Read(stream.ToArray(), e => new SkillChainRequest(e));

        Assert.AreEqual(52000711, back.Projectile.Skill);
        Assert.AreEqual((short)105, back.Projectile.TriggerIndex);
    }

    [TestMethod]
    public void TrapPlaceIsSkillAttackAndPosition()
    {
        var data = Written(new SkillTrapPosUpdateBroadcastResponse
        {
            Actor = new ActorValue(5, ActorType.User),
            Position = default(SkillTrapPosition) with { Skill = 24000111, TriggerIndex = 68, Position = new Vector3(4, 5, 6) }
        });

        Assert.HasCount(4 + 4 + 2 + 12, data);

        var back = Read(data, e => new SkillTrapPosUpdateBroadcastResponse(e));

        Assert.AreEqual(24000111, back.Position.Skill, "a skill id does not fit in a short");
        Assert.AreEqual((short)68, back.Position.TriggerIndex);
        Assert.AreEqual(new Vector3(4, 5, 6), back.Position.Position);
    }
}
