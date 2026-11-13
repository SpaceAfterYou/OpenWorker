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
    private static byte[] Written(IResponseHotspotMessage message) => WireBytes.Of(message);

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

        var bytes = WireBytes.Of(request);

        Assert.HasCount(38, bytes);

        var back = Read(bytes, e => new SkillChainRequest(e));

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

    /// <summary>
    /// The client sends 06/4C without a body to start charging and with the time the key was held to end it.
    /// </summary>
    [TestMethod]
    public void ChargingStartsWithNoBodyAndEndsWithTheTimeTheKeyWasHeld()
    {
        var start = Read([], e => new SkillChargingRequest(e));

        Assert.IsFalse(start.Released);

        var end = Read(BitConverter.GetBytes(1.25f), e => new SkillChargingRequest(e));

        Assert.IsTrue(end.Released);
        Assert.AreEqual(1.25f, end.PressTime);
    }

    /// <summary>
    /// The list of targets of a player starts with the player: the client reads its actor before the count.
    /// </summary>
    [TestMethod]
    public void PreTargetListIsThePlayerThenACountedListOfActors()
    {
        var data = Written(new SkillPreTargetListResponse
        {
            Actor = new ActorValue(5, ActorType.User),
            ActorList = [new ActorValue(7, ActorType.Monster), new ActorValue(8, ActorType.Monster)]
        });

        Assert.HasCount(4 + 1 + (2 * 4), data);
        Assert.AreEqual(2, data[4]);

        var back = Read(data, e => new SkillPreTargetListResponse(e));

        Assert.AreEqual(5, back.Actor.Identifier);
        Assert.AreEqual(8, back.ActorList[1].Identifier);
    }

    /// <summary>
    /// The answer to a card called into a fight is a code, then the card, the player and the place it was called from, as the
    /// client reads them; the coming of the card is its owner, the card, how it appears, its offset and the switch of its turn.
    /// </summary>
    [TestMethod]
    public void AkashicCallIsTheCodeTheCardThePlayerAndThePlace()
    {
        var data = Written(new SkillAkashicRecordResponse
        {
            Unknown = 0,
            Card = default(SkillAkashicRecordValue) with
            {
                Akashic = 381000101,
                Player = new ActorValue(5, ActorType.User),
                SkillPosition = default(SkillPositionValue) with { Position = new Vector3(1, 2, 3), Angle = 90f, MotionClass = 4, HasInputFlag = true }
            }
        });

        Assert.HasCount(2 + 4 + 4 + 12 + 4 + 2 + 1, data);
        Assert.AreEqual(381000101u, BitConverter.ToUInt32(data, 2));
        Assert.AreEqual(90f, BitConverter.ToSingle(data, 2 + 4 + 4 + 12));

        var summon = Written(new SkillSummonAkashicResponse
        {
            Owner = new ActorValue(5, ActorType.User),
            Creature = new ActorValue(9, ActorType.Akashic),
            AlphaValuea = 1f,
            BlendingTime = 1f,
            Position = new Vector3(0f, -350f, 0f),
            ApplyRotation = true
        });

        Assert.HasCount(4 + 4 + 4 + 4 + 12 + 1, summon);
        Assert.AreEqual(-350f, BitConverter.ToSingle(summon, 20));
        Assert.AreEqual(1, summon[28]);
    }
}
