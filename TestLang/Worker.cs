using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ow.Framework.IO.File.Bin;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestLang
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;

        private static async Task Test2()
        {
            await Task.Delay(2000);
            Console.WriteLine("asd test 2");
        }

        private static Task Test1()
        {
            return Test2();

            //await Task.Delay(2000);
            //Console.WriteLine("asd test");
        }

        public Worker(BinTable tables, ILogger<Worker> logger)
        {
            //var classSelectInfoTable = tables.ReadClassSelectInfoTable();
            //var customizeEyesTable = tables.ReadCustomizeEyesTable();
            //var customizeSkinTable = tables.ReadCustomizeSkinTable();
            //var customizeHairTable = tables.ReadCustomizeHairTable();
            //var itemTable = tables.ReadItemTable();
            //var itemClassifyTable = tables.ReadItemClassifyTable();
            //var customizeInfoTable = tables.ReadCustomizeInfoTable();
            //var characterInfoTable = tables.ReadCharacterInfoTable().Values;
            //var photoItemTable = tables.ReadPhotoItemTable().Values.Where(s => s.Hero == Hero.Haru).ToArray();
            //var gestureTable = tables.ReadGestureTable().Values;
            //var mazeInfoTable = tables.ReadMazeInfoTable();

            //var q = itemClassifyTable.Values.Where(s => s.GainType != 0).ToArray();
            //var q2 = itemClassifyTable.Values.Select(s => s.GainType).Distinct().ToArray();

            //var hero = characterInfoTable.First();
            //var maze = mazeInfoTable[(ushort)hero.Maze];

            //Test1().Wait();

            var passInfoTable = tables.ReadPassInfoTable();
            var passRewardInfoTable = tables.ReadPassRewardInfoTable().Values;

            Console.WriteLine("asd main");

            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}