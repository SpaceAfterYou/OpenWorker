using Microsoft.Extensions.Configuration;
using System.Net;

namespace GateService.Systems.GameSystem
{
    public sealed class District : IPEndPoint
    {
        public District(IConfigurationSection section) :
            base(IPAddress.Parse(section["Host:Ip"]), ushort.Parse(section["Host:Port"]))
        { }
    }
}