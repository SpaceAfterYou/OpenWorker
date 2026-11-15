using OpenWorker.Hotspot.Cache;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A reservation lets one connection in: the first that takes it away enters, the next finds none.
/// </summary>
[TestClass]
public sealed class ReservationsTest
{
    [TestMethod]
    public async Task AReservationIsTakenOnce()
    {
        var redis = await PartyTest.RedisAsync();
        var records = new RedisConnectionProvider(redis).RedisCollection<MazeReserveCache>(saveState: false);
        var person = PartyTest.People(1)[0];

        await records.InsertAsync(new MazeReserveCache { Person = person, Account = 1, Location = 21311, X = 0, Y = 0, Z = 0, R = 0, Jump = 0 }, TimeSpan.FromMinutes(1));

        Assert.IsNotNull(await records.FindByIdAsync(person.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        var reservations = new Reservations(redis);
        var taken = await Task.WhenAll(reservations.TakeAsync<MazeReserveCache>(person), reservations.TakeAsync<MazeReserveCache>(person));

        Assert.AreEqual(1, taken.Count(e => e), "Of two that come with one reservation, one enters");
        Assert.IsNull(await records.FindByIdAsync(person.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// A maze reservation names the maze server the person is sent to, and another maze server of the gate does not let the
    /// person in with it; one written when no maze server had registered lets any in.
    /// </summary>
    [TestMethod]
    public async Task AMazeReservationAdmitsOnlyItsServer()
    {
        var redis = await PartyTest.RedisAsync();
        var records = new RedisConnectionProvider(redis).RedisCollection<MazeReserveCache>(saveState: false);
        var people = PartyTest.People(2);
        var (named, any) = (people[0], people[1]);
        var server = Guid.NewGuid();

        await records.InsertAsync(new MazeReserveCache { Person = named, Account = 1, Location = 21311, X = 0, Y = 0, Z = 0, R = 0, Jump = 0, Server = server },
            TimeSpan.FromMinutes(1));
        await records.InsertAsync(new MazeReserveCache { Person = any, Account = 1, Location = 21311, X = 0, Y = 0, Z = 0, R = 0, Jump = 0 }, TimeSpan.FromMinutes(1));

        var read = await records.FindByIdAsync(named.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var anywhere = await records.FindByIdAsync(any.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.IsTrue(read!.Admits(server));
        Assert.IsFalse(read.Admits(Guid.NewGuid()), "a second maze server turns the person away");
        Assert.IsTrue(anywhere!.Admits(Guid.NewGuid()));
    }
}
