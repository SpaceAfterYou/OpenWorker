namespace OpenWorker.Havok.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class HavokSerializeClassAttribute(string name, byte version) : Attribute
{
    public string Name { get; } = name;
    public byte Version { get; } = version;
}