using Core.Systems.GameSystem.Datas.World.Table;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Xml;

namespace Core.Systems.GameSystem
{
    public sealed class WorldTableProcessor
    {
        public VRoot Read(string file)
        {
            using Stream stream = _data.GetInputStream(_data.GetEntry(file));

            XmlDocument xml = new();
            xml.Load(stream);

            return new(xml.DocumentElement);
        }

        public WorldTableProcessor(IConfiguration configuration) =>
            _data = new(configuration);

        private readonly VData49 _data;
    }
}