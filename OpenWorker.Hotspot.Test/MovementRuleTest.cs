using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the server does with the place a client says it has moved to, as the original server does: it refuses only a move
/// that takes the player too far in one message, counts the ones that are too fast, drops the ones of another map and the
/// ones the client sent before it took the place the server told it, and keeps what the client says of the ground.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MovementRuleTest
{
    private const long OtherMap = 0x0001_4E20_0000_0000;

    private static readonly Dictionary<Hero, float> Speeds = new() { [Hero.Lily] = 560f };

    /// <summary>
    /// A dash or a jump covers more ground than a hero runs in the time, and far less than the limit of one message: it is
    /// taken as it was sent and counted as too fast, nothing more.
    /// </summary>
    [TestMethod]
    public async Task AMoveThatIsTooFastButNotTooFarIsTakenAndCounted()
    {
        using var metrics = new MetricsHost();
        using var collector = metrics.Listen<long>("movement.too_fast");
        using var district = new DistrictHarness(Speeds, meters: metrics.Factory);

        var mover = district.Join(100f, 100f);

        // The first move spends the whole burst, the second one has nothing saved up.
        await district.MoveAsync(mover, 2600f, 100f);
        await district.MoveAsync(mover, 5100f, 100f);

        Assert.AreEqual(5100f, district.Ecs.Get<WorldComponent>(mover.Entity).Position.X, "A fast move is taken.");
        Assert.AreEqual(1, collector.Sum(), "It is counted.");
        Assert.AreEqual(0, district.Ecs.TryGet(mover.Entity, out HandlerErrorsComponent errors) ? errors.Count : 0, "It is no fault.");
    }

    /// <summary>
    /// The move of a client that still stands in the map it left names that map: the original drops it, whatever the place.
    /// </summary>
    [TestMethod]
    public async Task AMoveThatNamesAnotherMapIsDroppedWithoutAFault()
    {
        using var district = new DistrictHarness(Speeds);

        var mover = district.Join(100f, 100f);
        var observer = district.Join(200f, 100f);

        await district.ResetAsync(mover, observer);

        await district.MoveAsync(mover, 40_000f, 100f, OtherMap);
        await district.MoveAsync(mover, 300f, 100f, OtherMap);
        await district.SettleAsync(mover, observer);

        Assert.AreEqual(100f, district.Ecs.Get<WorldComponent>(mover.Entity).Position.X, "The place stays.");
        Assert.IsEmpty(mover.Received().Where(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.WarpRes)).ToArray(), "The player is not warped back.");
        Assert.IsEmpty(observer.Moves(), "Nobody hears of it.");
        Assert.AreEqual(0, district.Ecs.TryGet(mover.Entity, out HandlerErrorsComponent errors) ? errors.Count : 0);
    }

    /// <summary>
    /// After the server tells the client a place, the client sends the place of the map it left until it has taken the new
    /// one. Those moves are no fault; the first move from near the new place ends the grace, and a far move after that is
    /// refused. The grace is short: a client that keeps sending a far place is refused.
    /// </summary>
    [TestMethod]
    public async Task TheMovesOfThePlaceTheClientLeftAreDroppedUntilItTakesTheTold()
    {
        using var district = new DistrictHarness(Speeds, maxErrors: 10);

        var mover = district.Join(100f, 100f);

        district.Pace.PlaceTold(district.Ecs, mover.Entity);

        await district.MoveAsync(mover, 68_000f, 100f);
        district.Time.Advance(TimeSpan.FromSeconds(1));
        await district.MoveAsync(mover, 68_100f, 100f);

        Assert.AreEqual(100f, district.Ecs.Get<WorldComponent>(mover.Entity).Position.X, "The place stays.");
        Assert.AreEqual(0, district.Ecs.TryGet(mover.Entity, out HandlerErrorsComponent none) ? none.Count : 0, "No fault.");
        Assert.IsEmpty(mover.Received().Where(e => e.Is(GroupOpcode.World, (byte)WorldOpcode.WarpRes)).ToArray(), "No warp.");

        // The client takes the place: its next move is from there.
        await district.MoveAsync(mover, 140f, 100f);

        Assert.AreEqual(140f, district.Ecs.Get<WorldComponent>(mover.Entity).Position.X);

        await district.MoveAsync(mover, 68_000f, 100f);

        Assert.AreEqual(1, district.Ecs.Get<HandlerErrorsComponent>(mover.Entity).Count, "Once it has taken the place, a far move is refused.");

        // A place told and never taken: the grace is over.
        district.Pace.PlaceTold(district.Ecs, mover.Entity);
        district.Time.Advance(TimeSpan.FromSeconds(6));

        await district.MoveAsync(mover, 68_000f, 100f);

        Assert.AreEqual(2, district.Ecs.Get<HandlerErrorsComponent>(mover.Entity).Count, "After the grace a far move is refused.");
    }

    /// <summary>
    /// The client says whether the player stands on the ground, and the server keeps what it said, for its own player only.
    /// </summary>
    [TestMethod]
    public async Task TheGroundStateOfAPlayerIsKept()
    {
        using var district = new DistrictHarness(Speeds);

        var player = district.Join(100f, 100f);
        var other = district.Join(200f, 100f);

        await district.Movement.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), GroundStatus(player.Actor, 1, 230.5f));

        Assert.AreEqual(new GroundStateComponent(true, 230.5f), district.Ecs.Get<GroundStateComponent>(player.Entity));

        await district.Movement.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), GroundStatus(player.Actor, 0, 400f));

        Assert.AreEqual(new GroundStateComponent(false, 400f), district.Ecs.Get<GroundStateComponent>(player.Entity), "It follows the client.");

        await district.Movement.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), GroundStatus(other.Actor, 1, 10f));

        Assert.AreEqual(new GroundStateComponent(false, 400f), district.Ecs.Get<GroundStateComponent>(player.Entity), "Nobody says it for another.");
        Assert.AreEqual(default, district.Ecs.Get<GroundStateComponent>(other.Entity));
    }

    private static MovementGroundStatusRequest GroundStatus(int actor, int status, float height)
    {
        Span<byte> bytes = stackalloc byte[4 + 4 + 4];

        var writer = new PacketWriter(bytes);

        writer.WriteInt32(actor);
        writer.WriteInt32(status);
        writer.WriteSingle(height);

        var reader = new PacketReader(bytes);

        return MovementGroundStatusRequest.Read(ref reader);
    }
}
