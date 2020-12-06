using Core.Systems.GameSystem.Datas.World.Table;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Xml;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace Core.Systems.GameSystem
{
    public class WorldTableProcessor
    {
        public static VRoot Read(IConfiguration configuration, string file)
        {
            using var archive = new VArchive(Path.Join(configuration["Game:Dir"], "data49"));
            using var stream = archive[file].OpenReader();

            XmlDocument xml = new();
            xml.Load(stream);

            return new(xml.DocumentElement);
        }
    }
}