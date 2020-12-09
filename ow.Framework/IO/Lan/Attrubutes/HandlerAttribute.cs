using System;

namespace ow.Framework.IO.Lan.Attrubutes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandlerAttribute : Attribute
    {
        public string Channel { get; }

        public HandlerAttribute(string channel) =>
            Channel = channel;
    }
}
