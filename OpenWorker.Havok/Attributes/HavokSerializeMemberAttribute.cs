namespace OpenWorker.Havok.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class HavokSerializeMemberAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}