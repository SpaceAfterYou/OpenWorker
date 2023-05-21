using DefaultEcs;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Infrastructure.Communication.Hotspot.Handlers;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Abstractions;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Channels.Abstractions;
using OpenWorker.Infrastructure.Communication.Hotspot.Packet.Services;
using OpenWorker.Infrastructure.Communication.Hotspot.Server.Services;

namespace OpenWorker.Infrastructure.Communication.Hotspot.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddHotSpot(this IServiceCollection @this) => @this
        .AddScoped<HotSpotSession>()

        .AddScoped<PacketExecutor>()
        .AddScoped<PacketHandlerService>()

        .AddScoped<RawPacketPipe>()
        .AddScoped<IReadOnlyRawPacketPipe>(v => v.GetRequiredService<RawPacketPipe>())
        .AddScoped<IWriteOnlyRawPacketPipe>(v => v.GetRequiredService<RawPacketPipe>())

        .AddSingleton<World>()
        .AddSingleton<PacketReader>()
        .AddSingleton<HotSpotServer>()
        .AddSingleton<PacketExchanger>()
        .AddSingleton<HandlerCollection>()
        .AddSingleton<IPacketExchanger, PacketExchanger>()

        .AddHostedService<ServerHostedService>();
}
