using Microsoft.Extensions.Configuration;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ow.Utils.GenerateItemsForView
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string gamePath = args.ElementAt(0);
            string outPath = args.ElementAt(1);

            BinTable table = new(GetConfiguration());

            IReadOnlyDictionary<uint, ItemTableEntity> itemsTable = table.ReadItemTable();
            IReadOnlyDictionary<uint, ItemClassifyTableEntity> itemClassifyTable = table.ReadItemClassifyTable();
            IReadOnlyDictionary<uint, ItemScriptTableEntity> itemScriptTable = table.ReadItemScriptTable();

            var data = itemsTable.Values
                .Select(i =>
                {
                    if (itemScriptTable.TryGetValue(i.Id, out var script))
                    {
                        return new
                        {
                            i.Id,
                            itemClassifyTable[i.Classify].Type,
                            script.Name,
                            script.Description,
                            Icon = $"GUI/{script.Icon}.png",
                            i.Hero
                        };
                    }

                    return new
                    {
                        i.Id,
                        itemClassifyTable[i.Classify].Type,
                        Name = "",
                        Description = "",
                        Icon = "",
                        i.Hero
                    };
                });

            JsonSerializerOptions options = new()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            File.WriteAllText(outPath, JsonSerializer.Serialize(data, options));
        }

        private static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();
    }
}