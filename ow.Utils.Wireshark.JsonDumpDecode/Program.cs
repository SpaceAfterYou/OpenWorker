using ow.Framework;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ow.Utils.Wireshark.JsonDumpDecode
{
    //
    // USAGE
    //
    //   > ./executableFile wiresharkDump.json packets.txt 192.168.0.1
    //
    // WHERE
    //
    //   wiresharkDump.json
    //      Write full path, as example: c:\wiresharkDump.json
    //      Wireshark dump file (File > Export Packet Dissections > as JSON)
    //
    //   packets.txt
    //      Write full path, as example: c:\packets.txt
    //      Dump results (output)
    //
    //   192.168.0.1
    //      You IP address (at the time of the game)
    //

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            string inputFilePath = args.ElementAt(0);
            string outputFilePath = args.ElementAt(1);
            string clientIp = args.ElementAt(2);

            await using FileStream inputFile = new(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream outputFile = new(outputFilePath, FileMode.OpenOrCreate | FileMode.Truncate, FileAccess.Write, FileShare.Write);

            using JsonDocument json = await JsonDocument.ParseAsync(inputFile);
            using JsonElement.ArrayEnumerator enumerator = json.RootElement.EnumerateArray();

            foreach (JsonElement element in enumerator)
            {
                if (!element.TryGetProperty("_source", out JsonElement source))
                    throw new ApplicationException();

                if (!source.TryGetProperty("layers", out JsonElement layers))
                    throw new ApplicationException();

                if (!layers.TryGetProperty("tcp", out JsonElement tcp))
                    throw new ApplicationException();

                if (!layers.TryGetProperty("frame", out JsonElement frame))
                    throw new ApplicationException();

                if (!frame.TryGetProperty("frame.number", out JsonElement frameNumber))
                    throw new ApplicationException();

                Console.Write($"Process frame {frameNumber.GetString()}\n");

                // part of packet
                if (tcp.TryGetProperty("tcp.reassembled_in", out JsonElement tcpReassembledIn))
                {
                    Console.Write($"skipped (reassembled in {tcpReassembledIn.GetString()})\n");
                    continue;
                }

                JsonElement payload;
                if (layers.TryGetProperty("tcp.segments", out JsonElement tcpSegments) && tcpSegments.TryGetProperty("tcp.reassembled.data", out payload))
                { }
                else if (!tcp.TryGetProperty("tcp.payload", out payload))
                    continue;

                if (!layers.TryGetProperty("ip", out JsonElement ip))
                    throw new ApplicationException();

                if (!ip.TryGetProperty("ip.dst_host", out JsonElement dstHost))
                    throw new ApplicationException();

                if (!ip.TryGetProperty("ip.src_host", out JsonElement srcHost))
                    throw new ApplicationException();

                if (!frame.TryGetProperty("frame.time_relative", out JsonElement timeRelativeElement))
                    throw new ApplicationException();

                string ipDst = dstHost.GetString();
                string ipSrc = srcHost.GetString();
                string timeRelative = timeRelativeElement.GetString();

                await using MemoryStream streamMs = new(Convert.FromHexString(payload.GetString().Where(s => s != ':').ToArray()));
                using BinaryReader streamBr = new(streamMs);

                while (streamMs.Position != streamMs.Length)
                {
                    // packet may stick to another
                    if (streamBr.ReadByte() != 0x02 || streamBr.ReadByte() != 0x00)
                        continue;

                    ushort size = streamBr.ReadUInt16();
                    byte unknown = streamBr.ReadByte();

                    byte[] packet = streamBr.ReadBytes(size - Defines.PacketUnEncryptedHeaderSize);
                    PacketUtils.Exchange(ref packet);

                    await using MemoryStream packetMs = new(packet);
                    using BinaryReader packetBr = new(packetMs);

                    ushort rawOpcode = ConvertUtils.LeToBeUInt16(packetBr.ReadUInt16());

                    if (clientIp == ipDst)
                    {
                        await outputFile.WriteAsync(Encoding.ASCII.GetBytes($" [Client::{(Enum.IsDefined(typeof(ClientOpcode), rawOpcode) ? (ClientOpcode)rawOpcode : "???")}] {timeRelative}: "));
                        await outputFile.WriteAsync(Encoding.ASCII.GetBytes($"[{ipSrc}] ---> [{ipDst}]\n"));
                    }
                    else
                    {
                        await outputFile.WriteAsync(Encoding.ASCII.GetBytes($" [Server::{(Enum.IsDefined(typeof(ServerOpcode), rawOpcode) ? (ServerOpcode)rawOpcode : "???")}] {timeRelative}: "));
                        await outputFile.WriteAsync(Encoding.ASCII.GetBytes($"[{ipDst}] <--- [ {ipSrc}]\n"));
                    }

                    await outputFile.WriteAsync(Encoding.ASCII.GetBytes($"{BitConverter.ToString(packet).Replace('-', ' ')}\n\n"));
                }
            }

            Console.WriteLine("Hello World!");
        }
    }
}