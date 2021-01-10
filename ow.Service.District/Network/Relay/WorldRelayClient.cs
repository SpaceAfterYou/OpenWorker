using Microsoft.Extensions.Configuration;

namespace ow.Service.District.Network.Relay
{
    public sealed class WorldRelayClient
    {
        public PartyService.PartyServiceClient Party { get; }

        public WorldRelayClient(IConfiguration configuration)
        {
            Session = new(new RWChannel(configuration.GetSection("World:GlobalRelay:Host")));
        }

        //public WorldRelayClient(IConfiguration configuration) =>
        //    Session = new(new WorldRelayChannel(GetHost(configuration, configuration["World"])));

        //private static IConfigurationSection GetHost(IConfiguration configuration, string world) =>
        //    configuration.GetSection($"World:Instance:{world}:Relay:Host");
    }
}

// https://youtu.be/BCHRxsmWsCQ