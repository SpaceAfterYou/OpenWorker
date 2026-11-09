using Arch.Core;

namespace OpenWorker.Hotspot;

/// <summary>
/// Names what the server has learnt about a session in the scope of its log lines (<see cref="SessionLogScope"/>).
/// Called on the world loop where the server learns it; an entity without a session (a test, a person of the gate) is
/// left alone.
/// </summary>
public static class SessionLogScopeExtensions
{
    public static void SetLogAccount(this World world, Entity entity, int account)
    {
        if (TryGetScope(world, entity) is { } scope)
        {
            scope.Account = account;
        }
    }

    public static void SetLogPerson(this World world, Entity entity, int person)
    {
        if (TryGetScope(world, entity) is { } scope)
        {
            scope.Person = person;
        }
    }

    private static SessionLogScope? TryGetScope(World world, Entity entity)
    {
        return world.IsAlive(entity) && world.TryGet(entity, out ServerSessionComponent session) ? session.LogScope : null;
    }
}
