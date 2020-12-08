using System;

namespace Core.Systems.LanSystem.Attrubutes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandlerAttribute : Attribute
    {
        public string Channel { get; }

        public HandlerAttribute(string channel) =>
            Channel = channel;
    }
}
