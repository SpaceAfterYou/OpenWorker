﻿using Microsoft.Extensions.Configuration;

namespace GateService.Systems.GameSystem
{
    public sealed class District
    {
        public string Ip { get; }
        public ushort Port { get; }

        public District(IConfigurationSection section)
        {
            Ip = section["Host:Ip"];
            Port = ushort.Parse(section["Host:Port"]);
        }
    }
}