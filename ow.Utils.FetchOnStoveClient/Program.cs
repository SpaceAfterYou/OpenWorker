using ow.Utils.FetchOnStoveClient.Results;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ow.Utils.FetchOnStoveClient
{
    internal class Program
    {
        private enum LineToken : byte
        {
            Id,
            FileType,
            FilePath,
            Version,
            WebPath,
            WebExtension,
            Int1,
            Int2,
            Hash1,
            Hash2,
            Int3,
            Unknow1,
        }

        private static async Task Main(string[] args)
        {
            string host = args.ElementAt(0);
            ushort port = ushort.Parse(args.ElementAt(1));

            // ClientFilesResult? files2 = await OnStove.GetClientFiles(host, port);
            //return;

            //await using FileStream fileStream = File.OpenRead(@"C:\Users\sawic\source\repos\OpenWorker\11_238.json");
            await using FileStream fileStream = File.OpenRead(@"C:\Users\sawic\Desktop\11_1.json");
            ClientFilesResult? files = await JsonSerializer.DeserializeAsync<ClientFilesResult>(fileStream);

            string outputPath = Path.Join(args.ElementAt(2), files?.RootFolder);
            Directory.CreateDirectory(outputPath);

            using HttpClient http = new()
            {
                BaseAddress = new($"http://sw.cdn.stovegame.net/game/dpms_{OnStove.SoulWorkerGameCode}/")
            };

            List<Task> Downloads = new();

            foreach (string line in files?.Files!)
            {
                string[] splittedLine = line.Split("|").Select(s => s.Trim()).ToArray();
                switch (splittedLine[(byte)LineToken.FileType][0])
                {
                    case 'D':
                        {
                            string path = Path.Join(outputPath, splittedLine[(byte)LineToken.FilePath]);
                            Console.WriteLine($"Created directory: {path}");

                            Directory.CreateDirectory(path);

                            break;
                        }

                    case 'F':
                        {
                            Downloads.Add(Task.Run(async () =>
                            {
                                Debug.Assert(splittedLine[(byte)LineToken.WebExtension] == "gz");

                                string url = $"v{splittedLine[(byte)LineToken.Version]}/{splittedLine[(byte)LineToken.WebPath]}.{splittedLine[(byte)LineToken.WebExtension]}";
                                string path = Path.Join(outputPath, splittedLine[(byte)LineToken.FilePath]);

                                // var (left, top) = Console.GetCursorPosition();

                                while (true)
                                {
                                    try
                                    {
                                        Console.WriteLine($"ProcessFile: {splittedLine[(byte)LineToken.FilePath]} ({url})");

                                        await using Stream stream = await http.GetStreamAsync(url);
                                        await using GZipStream decompressionStream = new(stream, CompressionMode.Decompress);

                                        await using FileStream outputFile = new(path, FileMode.OpenOrCreate, FileAccess.Write);
                                        await decompressionStream.CopyToAsync(outputFile);

                                        break;
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine($"Error!: {splittedLine[(byte)LineToken.FilePath]} ({url})");
                                        Console.WriteLine(e);
                                    }
                                }
                            }));

                            break;
                        }
                }
            }

            Console.WriteLine("Wait downloads...");

            await Task.WhenAll(Downloads);

            Console.WriteLine("Hello World!");
        }
    }
}