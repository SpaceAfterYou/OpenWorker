using Core.Systems.GameSystem.Datas.World.Table;
using System;
using System.IO;
using System.Xml;

namespace LangTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using FileStream fs = new(@"Y:\soulworker-dev\swe\1\World\Table\T02_TUTORIAL.vbatch", FileMode.Open);

            XmlDocument xml = new();
            xml.Load(fs);

            var q = new VRoot(xml.DocumentElement);

            Console.WriteLine("Hello World!");
        }
    }
}