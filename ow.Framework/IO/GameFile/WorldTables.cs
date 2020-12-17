using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.World.Table;
using ow.Framework.IO.GameFile;
using System.IO;
using System.Xml;

namespace ow.Framework.Game
{
    public sealed class WorldTables
    {
        public VRoot Read(string file)
        {
            using Stream stream = _data.GetInputStream(_data.GetEntry($"World/Table/{file}.vbatch"));

            XmlDocument xml = new();
            xml.Load(stream);

            return new(xml.DocumentElement);
        }

        public WorldTables(IConfiguration configuration) =>
            _data = new(configuration);

        private readonly VData49 _data;
    }
}