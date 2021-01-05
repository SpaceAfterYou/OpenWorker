using System;

namespace ow.Utils.Wireshark.JsonDumpDecode
{
    [Serializable]
    public sealed class DecoderException : Exception
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
    }
}