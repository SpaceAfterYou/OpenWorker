using OpenWorker.Hotspot.Cache.Types;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class DistrictReserveCacheTest
{
    private static DistrictReserveCache Reserve(int person, Guid district) => new()
    {
        Account = 1,
        Person = person,
        Channel = Guid.NewGuid(),
        District = district,
        X = 1,
        Y = 2,
        Z = 3,
        R = 4,
        Jump = 5
    };

    [TestMethod]
    public void ReservationAdmitsItsPersonToItsDistrict()
    {
        var district = Guid.NewGuid();

        Assert.IsTrue(Reserve(7, district).Admits(7, district));
    }

    [TestMethod]
    public void ReservationDoesNotAdmitAnotherPerson()
    {
        var district = Guid.NewGuid();

        Assert.IsFalse(Reserve(7, district).Admits(8, district));
    }

    [TestMethod]
    public void ReservationDoesNotAdmitToAnotherDistrict()
    {
        Assert.IsFalse(Reserve(7, Guid.NewGuid()).Admits(7, Guid.NewGuid()));
    }
}
