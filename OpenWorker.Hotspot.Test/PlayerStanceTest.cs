using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Persons.Requests;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What the client of a player may say of the way it lies after a throw of the server.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerStanceTest
{
    private static readonly PlayerPostureComponent Lying = new(TimeSpan.Zero, TimeSpan.FromSeconds(4));

    /// <summary>
    /// The client says its player gets up: it is up at once, though the server would let it lie longer.
    /// </summary>
    [TestMethod]
    public async Task TheClientGetsItsPlayerUpAtOnce()
    {
        using var district = new DistrictHarness();
        var player = district.Join(0f, 0f);

        district.Ecs.Add(player.Entity, Lying);

        await TellAsync(district, player, MotionClass.DownUp, 2);

        Assert.IsTrue(PlayerStance.IsLying(district.Ecs.Get<PlayerPostureComponent>(player.Entity), TimeSpan.FromSeconds(1)), "Lying only confirms.");

        await TellAsync(district, player, MotionClass.DownUp, PlayerStance.Rising);

        Assert.IsFalse(PlayerStance.IsLying(district.Ecs.Get<PlayerPostureComponent>(player.Entity), TimeSpan.FromSeconds(1)), "Up.");
    }

    /// <summary>
    /// A client that says its player lies, when no throw of the server put it there, makes nobody lie.
    /// </summary>
    [TestMethod]
    public async Task AClientsClaimAloneMakesNobodyLie()
    {
        using var district = new DistrictHarness();
        var player = district.Join(0f, 0f);

        await TellAsync(district, player, MotionClass.DownUp, 2);

        Assert.IsFalse(district.Ecs.Has<PlayerPostureComponent>(player.Entity));
    }

    private static ValueTask TellAsync(DistrictHarness district, Player player, short motion, short step)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(motion);
        writer.Write(step);
        writer.Flush();
        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return district.Movement.OnHandleAsync(new ServiceHandleContext(player.Entity, CancellationToken.None), new PersonChangeMotionReqRequest(reader));
    }
}
