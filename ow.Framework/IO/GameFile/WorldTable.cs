using ow.Framework.Game.Datas.World.Table;
using Microsoft.Extensions.Configuration;
using ow.Framework.IO.GameFile;
using System.IO;
using System.Xml;

namespace ow.Framework.Game
{
    public sealed class WorldTable
    {
        public VRoot Read(string file)
        {
            using Stream stream = _data.GetInputStream(_data.GetEntry(file));

            XmlDocument xml = new();
            xml.Load(stream);

            return new(xml.DocumentElement);
        }

        public WorldTable(IConfiguration configuration) =>
            _data = new(configuration);

        private readonly VData49 _data;
    }
}
