using Microsoft.Extensions.DependencyInjection;

namespace OpenWorker.Gameplay.Modules.MyRoom;

public static class MyRoomServiceCollectionExtensions
{
    /// <summary>
    /// The rooms of the accounts: the tables and the book every person carries its room in. The server that runs the rooms
    /// adds what keeps them while somebody is in them.
    /// </summary>
    public static IServiceCollection AddMyRoom(this IServiceCollection services)
    {
        services.AddSingleton<MyRoomCatalog>();
        services.AddSingleton<MyRoomBook>();

        return services;
    }
}
