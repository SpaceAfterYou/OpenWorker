using System;

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
    // NOTE
    //   filter trash packet's like: (tcp.port eq 10200 or tcp.port eq 10100 or tcp.port eq 10000)
    //
    //      10000
    //         auth server
    //
    //      10100
    //         gate server
    //
    //      10200
    //         Rocco Town server
    //         1 - 10 channels

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