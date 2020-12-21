using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.World.Table;
using System;
using System.IO;
using System.Xml;

namespace ow.Framework.IO.File.World
{
    public sealed class WorldTable
    {
        private readonly WorldFile _data;

        public VRoot ReadBatch(string file)
        {
            using Stream stream = _data.GetInputStream(_data.GetEntry($"World/Table/{file}.vbatch"));

            XmlDocument xml = new();
            xml.Load(stream);

            return new(xml.DocumentElement ?? throw new ApplicationException());
        }

        public WorldTable(IConfiguration configuration) => _data = new(configuration);
    }
}