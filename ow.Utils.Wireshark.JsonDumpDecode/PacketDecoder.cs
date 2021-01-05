using ow.Framework;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ow.Utils.Wireshark.JsonDumpDecode
{
    public sealed partial class PacketDecoder
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

            IEnumerable<RawPacket> rawPackets = enumerator
                .Select(element =>
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

                    string id = frameNumber.GetString();
                    Messages.ProcessFrame(id);

                    if (!layers.TryGetProperty("ip", out JsonElement ip))
                        throw new DecoderException();

                    if (!ip.TryGetProperty("ip.dst_host", out JsonElement dstHost))
                        throw new DecoderException();

                    if (!ip.TryGetProperty("ip.src_host", out JsonElement srcHost))
                        throw new DecoderException();

                    if (!frame.TryGetProperty("frame.time_relative", out JsonElement timeRelativeElement))
                        throw new DecoderException();

                    if (!tcp.TryGetProperty("tcp.payload", out JsonElement payload))
                        return null;

                    return new RawPacket()
                    {
                        Frame = id,
                        DstIp = dstHost.GetString(),
                        SrcIp = srcHost.GetString(),
                        RelativeTime = timeRelativeElement.GetString(),
                        Payload = payload.GetString()
                    };
                })
                .Where(s => s is not null);

            IEnumerable<string> keys = rawPackets.Select(s => $"{s.DstIp}-{s.SrcIp}").Distinct();

            IEnumerable<IEnumerable<RawPacket>> splittedPackets = keys.Select(k =>
            {
                string[] ips = k.Split('-');
                return rawPackets.Where(s => s.DstIp == ips[0] && s.SrcIp == ips[1]);
            });

            List<Packet> justPackets = new(rawPackets.Count());

            foreach (var packets in splittedPackets)
            {
                using MemoryStream ms = new(ushort.MaxValue);
                using BinaryReader br = new(ms);

                foreach (var packet in packets)
                {
                    long currentPos = ms.Position;
                    ms.Position = ms.Length;

                    ms.Write(Convert.FromHexString(packet.Payload.Where(s => s != ':').ToArray()));
                    ms.Position = currentPos;

                    bool nf = false;
                    while (ms.Position != ms.Length)
                    {
                        if (br.ReadByte() != 0x02 || br.ReadByte() != 0x00)
                        {
                            if (!nf)
                            {
                                nf = true;
                                Messages.Error($"Magic not found [Frame: {packet.Frame}]");
                            }
                            continue;
                        }

                        nf = false;

                        ushort size = br.ReadUInt16();
                        byte unknown = br.ReadByte();

                        if (size - Defines.PacketUnEncryptedHeaderSize > ms.Length - ms.Position)
                        {
                            ms.Position -= 5;
                            break;
                        }

                        PacketUtils.Exchange(ms.GetBuffer(), (int)ms.Position, size - Defines.PacketUnEncryptedHeaderSize);

                        justPackets.Add(new Packet()
                        {
                            Frame = long.Parse(packet.Frame),
                            Receiver = packet.DstIp,
                            Sender = packet.SrcIp,
                            Time = packet.RelativeTime,
                            Body = br.ReadBytes(size - Defines.PacketUnEncryptedHeaderSize)
                        });
                    }
                }
            }

            justPackets.Sort((a, b) => a.Frame.CompareTo(b.Frame));

            foreach (var packet in justPackets)
            {
                using MemoryStream ms = new(packet.Body);
                using BinaryReader br = new(ms);

                ushort rawOpcode = ConvertUtils.LeToBeUInt16(br.ReadUInt16());

                Messages.ExtracteOpcode(rawOpcode);

                if (clientIp == packet.Receiver)
                {
                    outputFile.Write(Encoding.ASCII.GetBytes($"{packet.Frame,4}   | [Client::{(Enum.IsDefined(typeof(ClientOpcode), rawOpcode) ? (ClientOpcode)rawOpcode : "???")}] {packet.Time}: "));
                    outputFile.Write(Encoding.ASCII.GetBytes($"[{packet.Sender}] ---> [{packet.Receiver}]\n"));
                }
                else
                {
                    outputFile.Write(Encoding.ASCII.GetBytes($"{packet.Frame,4}   | [Server::{(Enum.IsDefined(typeof(ServerOpcode), rawOpcode) ? (ServerOpcode)rawOpcode : "???")}] {packet.Time}: "));
                    outputFile.Write(Encoding.ASCII.GetBytes($"[{packet.Receiver}] <--- [ {packet.Sender}]\n"));
                }

                outputFile.Write(Encoding.ASCII.GetBytes($"{BitConverter.ToString(packet.Body).Replace('-', ' ')}\n\n"));
            }
        }
    }
}