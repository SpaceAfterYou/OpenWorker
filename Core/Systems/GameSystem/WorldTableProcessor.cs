using Core.Systems.GameSystem.Datas.World.Table;
using System.Xml;
using TrinigyVisionEngine.Vision.Runtime.Base.IO.Serialization;

namespace Core.Systems.GameSystem
{
    public class WorldTableProcessor
    {
        public static VRoot LoadBatch(string archiveName, string fileName)
        {
            using var archive = new VArchive(archiveName);
            using var stream = archive[fileName].OpenReader();

            XmlDocument xml = new();
            xml.Load(stream);

            return new(xml.DocumentElement);
        }
    }
}