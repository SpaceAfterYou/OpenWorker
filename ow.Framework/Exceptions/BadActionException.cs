using System;

namespace ow.Framework.Exceptions
{
    public sealed class BadActionException : Exception
    {
        public BadActionException() : base()
        {
        }

        public BadActionException(string? message) : base(message)
        {
        }

        public BadActionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}