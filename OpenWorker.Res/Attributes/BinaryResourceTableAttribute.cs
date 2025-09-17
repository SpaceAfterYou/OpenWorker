namespace OpenWorker.Res.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class BinaryResourceTableAttribute(string table) : Attribute
{
    public string Table { get; } = table;
}