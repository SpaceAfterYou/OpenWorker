using Microsoft.Extensions.Configuration;
using ow.Framework.Game.Datas.Bin.Table.Entities;
using ow.Framework.Game.Enums;
using ow.Framework.IO.File.Bin;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace ow.Utils.GenerateItemsForView
{
    public static class Program
    {
        public static Task Main(string[] args)
        {
            string gameDir = args.ElementAt(0);
            string outDir = args.ElementAt(1);

            using BinTable table = new(GetConfiguration());

            IReadOnlyDictionary<uint, ItemTableEntity> itemsTable = table.ReadItemTable();
            IReadOnlyDictionary<uint, ItemClassifyTableEntity> itemClassifyTable = table.ReadItemClassifyTable();
            IReadOnlyDictionary<uint, ItemScriptTableEntity> itemScriptTable = table.ReadItemScriptTable();

            IEnumerable<ItemClassifyInventoryType> inventoryTypes = itemClassifyTable.Values.Select(s => s.InventoryType).Distinct().OrderBy(s => s);
            IEnumerable<ItemClassifySlotType> slotTypes = itemClassifyTable.Values.Select(s => s.SlotType).Distinct().OrderBy(s => s);

            var data = itemsTable.Values
                .Select(i =>
                {
                    ItemClassifyTableEntity classify = itemClassifyTable[i.Classify];

                    if (itemScriptTable.TryGetValue(i.Id, out ItemScriptTableEntity script))
                    {
                        return new
                        {
                            i.Id,
                            classify.SlotType,
                            classify.InventoryType,
                            script.Name,
                            script.Description,
                            Icon = $"GUI/{script.Icon}.png",
                            i.Hero
                        };
                    }

                    return new
                    {
                        i.Id,
                        classify.SlotType,
                        classify.InventoryType,
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

            return Task.WhenAll(
                File.WriteAllTextAsync(Path.Join(outDir, "data.json"), JsonSerializer.Serialize(data, options)),
                File.WriteAllTextAsync(Path.Join(outDir, "inventoryTypes.json"), JsonSerializer.Serialize(inventoryTypes, options)),
                File.WriteAllTextAsync(Path.Join(outDir, "slotTypes.json"), JsonSerializer.Serialize(slotTypes, options)));
        }

        private static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config/appsettings.json", false, true)
            .Build();
    }
}