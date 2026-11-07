namespace OpenWorker.Res.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class BinaryResourceTableAttribute(string table) : Attribute
{
    public string Table { get; } = table;

    /// <summary>
    /// The table is not part of the client files and may be missing: the server then goes without what it gives.
    /// </summary>
    public bool Optional { get; init; }
}
