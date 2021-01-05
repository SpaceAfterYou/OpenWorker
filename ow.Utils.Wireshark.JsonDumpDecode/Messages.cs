using System;

namespace ow.Utils.Wireshark.JsonDumpDecode
{
    public static class Messages
    {
        public static void ExtracteOpcode(ushort opcode)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Extracted Opcode: 0x{opcode:X4}");
        }

        public static void ProcessFrame(string id)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Process Frame: {id}");
        }

        public static void Error(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
        }
    }
}