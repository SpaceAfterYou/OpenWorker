using Core.Systems.GameSystem.Datas.World.Table;
using System;
using System.IO;
using System.Xml.Serialization;

namespace LangTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using FileStream fs = new(@"Y:\soulworker-dev\swe\1\World\Table\T02_TUTORIAL.vbatch", FileMode.Open);

            XmlSerializer xml = new(typeof(VRoot));
            var q = (VRoot)xml.Deserialize(fs);

            Console.WriteLine("Hello World!");
        }
    }
}