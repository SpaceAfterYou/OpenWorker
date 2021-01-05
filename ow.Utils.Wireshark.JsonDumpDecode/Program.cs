using ow.Framework;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ow.Utils.Wireshark.JsonDumpDecode
{
    [Serializable]
    public class DecoderException : Exception
    {
        public DecoderException()
        {
        }

        public DecoderException(string message) : base(message)
        {
        }

        public DecoderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DecoderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public static class Messages
    {
        public static void ExtracteOpcode(ushort opcode)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Extracted opcode: 0x{opcode:X4}\n");
        }
    }

    public sealed class PacketDecoder
    {
        public void DoIt(string[] args)
        {
            string inputFilePath = args.ElementAt(0);
            string outputFilePath = args.ElementAt(1);
            string clientIp = args.ElementAt(2);

            using FileStream inputFile = new(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream outputFile = new(outputFilePath, FileMode.OpenOrCreate | FileMode.Truncate, FileAccess.Write, FileShare.Write);

            using JsonDocument json = JsonDocument.Parse(inputFile);
            using JsonElement.ArrayEnumerator enumerator = json.RootElement.EnumerateArray();

            Dictionary<string, MemoryStream> deferred = new();

            foreach (JsonElement element in enumerator)
            {
                if (!element.TryGetProperty("_source", out JsonElement source))
                    throw new DecoderException();

                if (!source.TryGetProperty("layers", out JsonElement layers))
                    throw new DecoderException();

                if (!layers.TryGetProperty("tcp", out JsonElement tcp))
                    throw new DecoderException();

                if (!layers.TryGetProperty("frame", out JsonElement frame))
                    throw new DecoderException();

                if (!frame.TryGetProperty("frame.number", out JsonElement frameNumber))
                    throw new DecoderException();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"Process frame {frameNumber.GetString()}\n");

                // part of packet
                if (tcp.TryGetProperty("tcp.reassembled_in", out JsonElement tcpReassembledIn))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"skipped (reassembled in {tcpReassembledIn.GetString()})\n");
                    continue;
                }

                if (layers.TryGetProperty("tcp.segments", out JsonElement tcpSegments) && tcpSegments.TryGetProperty("tcp.reassembled.data", out JsonElement payload))
                { }
                else if (!tcp.TryGetProperty("tcp.payload", out payload))
                    continue;

                if (!layers.TryGetProperty("ip", out JsonElement ip))
                    throw new DecoderException();

                if (!ip.TryGetProperty("ip.dst_host", out JsonElement dstHost))
                    throw new DecoderException();

                if (!ip.TryGetProperty("ip.src_host", out JsonElement srcHost))
                    throw new DecoderException();

                if (!frame.TryGetProperty("frame.time_relative", out JsonElement timeRelativeElement))
                    throw new DecoderException();

                string ipDst = dstHost.GetString();
                string ipSrc = srcHost.GetString();
                string timeRelative = timeRelativeElement.GetString();

                using MemoryStream ms = new(Convert.FromHexString(payload.GetString().Where(s => s != ':').ToArray()));
                using BinaryReader br = new(ms);

                while (ms.Position != ms.Length)
                {
                    // packet may stick to another
                    if (br.ReadByte() != 0x02 || br.ReadByte() != 0x00)
                    {
                        PushDeferred(frameNumber.GetString(), ipDst, ipSrc, ms, (int)(ms.Position - 2), (int)(ms.Length - ms.Position + 2));
                        break;
                    }

                    ushort size = br.ReadUInt16();
                    byte unknown = br.ReadByte();

                    long l1 = (ms.Length - ms.Position);
                    long l2 = (size - Defines.PacketUnEncryptedHeaderSize);
                    if (l1 < l2)
                    {
                        PushDeferred(frameNumber.GetString(), ipDst, ipSrc, ms, (int)(ms.Position - Defines.PacketUnEncryptedHeaderSize), (int)(ms.Length - ms.Position + Defines.PacketUnEncryptedHeaderSize));
                        break;
                    }

                    byte[] packet = br.ReadBytes(size - Defines.PacketUnEncryptedHeaderSize);
                    if (packet.Length != (size - Defines.PacketUnEncryptedHeaderSize))
                        throw new DecoderException();

                    PacketUtils.Exchange(ref packet);

                    using MemoryStream packetMs = new(packet);
                    using BinaryReader packetBr = new(packetMs);

                    ushort rawOpcode = ConvertUtils.LeToBeUInt16(packetBr.ReadUInt16());

                    Messages.ExtracteOpcode(rawOpcode);

                    if (clientIp == ipDst)
                    {
                        outputFile.Write(Encoding.ASCII.GetBytes($" [Client::{(Enum.IsDefined(typeof(ClientOpcode), rawOpcode) ? (ClientOpcode)rawOpcode : "???")}] {timeRelative}: "));
                        outputFile.Write(Encoding.ASCII.GetBytes($"[{ipSrc}] ---> [{ipDst}]\n"));
                    }
                    else
                    {
                        outputFile.Write(Encoding.ASCII.GetBytes($" [Server::{(Enum.IsDefined(typeof(ServerOpcode), rawOpcode) ? (ServerOpcode)rawOpcode : "???")}] {timeRelative}: "));
                        outputFile.Write(Encoding.ASCII.GetBytes($"[{ipDst}] <--- [ {ipSrc}]\n"));
                    }

                    outputFile.Write(Encoding.ASCII.GetBytes($"{BitConverter.ToString(packet).Replace('-', ' ')}\n\n"));
                }
            }

            ProcessDeferreds(clientIp, outputFile);
        }

        private void ProcessDeferreds(string clientIp, FileStream outputFile)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Process deferred");

            foreach (var item in deferred)
            {
                string[] ips = item.Key.Split('-');
                string ipDst = ips[0];
                string ipSrc = ips[1];

                using MemoryStream ms = item.Value;
                using BinaryReader br = new(ms);

                File.WriteAllBytes($@"Y:\soulworker-dev\swSniffer\wireshark\1\deferred\{item.Key}.bin", item.Value.ToArray());

                ms.Position = 0;

                int i = 0;

                while (ms.Position != ms.Length)
                {
                    // Console.ForegroundColor = ConsoleColor.Cyan;
                    // Console.WriteLine($"Process Deferred: [{ipDst}] - [{ipSrc}] : {i++}");

                    // packet may stick to another
                    if (br.ReadByte() != 0x02 || br.ReadByte() != 0x00)
                        continue;

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Process Deferred: [{ipDst}] - [{ipSrc}] : {i++}");

                    //throw new DecoderException();

                    ushort size = br.ReadUInt16();
                    byte unknown = br.ReadByte();

                    byte[] packet = br.ReadBytes(size - Defines.PacketUnEncryptedHeaderSize);
                    if (packet.Length != (size - Defines.PacketUnEncryptedHeaderSize))
                        throw new DecoderException();

                    PacketUtils.Exchange(ref packet);

                    using MemoryStream packetMs = new(packet);
                    using BinaryReader packetBr = new(packetMs);

                    ushort rawOpcode = ConvertUtils.LeToBeUInt16(packetBr.ReadUInt16());
                    Messages.ExtracteOpcode(rawOpcode);

                    if (clientIp == ipDst)
                    {
                        outputFile.Write(Encoding.ASCII.GetBytes($" [Client::{(Enum.IsDefined(typeof(ClientOpcode), rawOpcode) ? (ClientOpcode)rawOpcode : "???")}] {0}: "));
                        outputFile.Write(Encoding.ASCII.GetBytes($"[{ipSrc}] ---> [{ipDst}]\n"));
                    }
                    else
                    {
                        outputFile.Write(Encoding.ASCII.GetBytes($" [Server::{(Enum.IsDefined(typeof(ServerOpcode), rawOpcode) ? (ServerOpcode)rawOpcode : "???")}] {0}: "));
                        outputFile.Write(Encoding.ASCII.GetBytes($"[{ipDst}] <--- [ {ipSrc}]\n"));
                    }

                    outputFile.Write(Encoding.ASCII.GetBytes($"{BitConverter.ToString(packet).Replace('-', ' ')}\n\n"));
                }
            }
        }

        private void PushDeferred(string id, string dst, string src, MemoryStream ms, int offset, int limit)
        {
            Console.Write($"PushDeferred frame {id} [{dst}] - [{src}]\n");

            byte[] skipped = ms.ToArray().Skip(offset).Take(limit).ToArray();

            string key = $"{dst}-{src}";
            if (!deferred.TryGetValue(key, out MemoryStream skippedMs))
            {
                skippedMs = new();
                deferred.Add(key, skippedMs);
            }

            skippedMs.Write(skipped);
        }

        private readonly Dictionary<string, MemoryStream> deferred = new();
    }

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
        private static void Main(string[] args)
        {
            PacketDecoder packetDecoder = new();
            packetDecoder.DoIt(args);

            Console.WriteLine("Hello World!");
        }
    }
}