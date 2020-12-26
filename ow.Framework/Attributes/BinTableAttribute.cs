using System;

namespace ow.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class BinTableAttribute : Attribute
    {
        internal string Name { get; init; } = string.Empty;

        internal BinTableAttribute()
        {
        }

        internal BinTableAttribute(string name) => Name = name;
    }
}